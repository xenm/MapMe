# MapMe Authentication System

This document provides comprehensive information about MapMe's authentication system, including JWT authentication,
Google OAuth integration, new user flow, and profile prefilling.

## Table of Contents

1. [Overview](#overview)
2. [JWT Authentication](#jwt-authentication)
3. [Google OAuth Integration](#google-oauth-integration)
4. [Authentication Flow](#authentication-flow)
5. [New User Detection and Profile Prefilling](#new-user-detection-and-profile-prefilling)
6. [Configuration](#configuration)
7. [Security Considerations](#security-considerations)
8. [Testing and Validation](#testing-and-validation)
9. [Troubleshooting](#troubleshooting)
10. [Production Deployment](#production-deployment)

## Overview

MapMe uses a modern authentication system combining:

- **JWT (JSON Web Tokens)** for stateless authentication
- **Google OAuth 2.0** for social login
- **Intelligent user flow** with new user detection
- **Automatic profile prefilling** from Google data
- **Smart redirect logic** based on user status

### Architecture

MapMe follows a **client-server architecture**:

- **Server**: ASP.NET Core Web API (token issuer)
- **Client**: Blazor WebAssembly (token consumer)
- **Authentication**: Stateless JWT-based system
- **Social Login**: Google OAuth 2.0 integration

## JWT Authentication

### Key Features

- **HMAC SHA256** signing algorithm
- **Configurable expiration** (default 24 hours)
- **Claims-based** user identification
- **Automatic token refresh** capabilities
- **Stateless authentication** (no server-side sessions)

### Token Claims

| Claim              | Description                | Example              |
|--------------------|----------------------------|----------------------|
| `sub` (Subject)    | User ID                    | `"user-123-456"`     |
| `name`             | Username                   | `"johndoe"`          |
| `email`            | User email                 | `"john@example.com"` |
| `iat` (Issued At)  | Token creation timestamp   | `1640995200`         |
| `exp` (Expiration) | Token expiration timestamp | `1641081600`         |

### JWT Configuration

**Required Configuration:**
```json
{
  "Jwt": {
    "SecretKey": "your-jwt-secret-key-minimum-32-characters",
    "ExpirationHours": 24,
    "Issuer": "MapMe-Server",
    "Audience": "MapMe-Client"
  }
}
```

**Configuration Rationale:**

- **Issuer (`"MapMe-Server"`)**: Identifies the ASP.NET Core server that issues tokens
- **Audience (`"MapMe-Client"`)**: Identifies the Blazor WebAssembly client as intended recipient
- **Security Separation**: Clear distinction between token issuer and consumer

**User Secrets Setup (Development):**

```bash
dotnet user-secrets set "Jwt:SecretKey" "your-secure-jwt-secret-key-here"
```

## Google OAuth Integration

MapMe's Google OAuth integration provides seamless user authentication with automatic profile data extraction,
including:

- **User identity verification** through Google's secure OAuth 2.0 flow
- **Automatic profile prefilling** with name, email, and profile picture
- **Smart user detection** for new vs. existing users
- **Secure token validation** and user account creation

### Prerequisites

1. **Google Cloud Project** with billing enabled
2. **Google Identity Services API** enabled
3. **OAuth 2.0 Client ID** configured

### OAuth Client Setup

1. Open [Google Cloud Console](https://console.cloud.google.com/)
2. Navigate to **APIs & Services → Credentials**
3. Create **OAuth client ID** (Web application)
4. Configure authorized origins and redirect URIs:

**Development:**

```
Authorized JavaScript origins: https://localhost:8008
Authorized redirect URIs: https://localhost:8008/signin-google
```

**Production:**

```
Authorized JavaScript origins: https://yourdomain.com
Authorized redirect URIs: https://yourdomain.com/signin-google
```

### Google Configuration

```json
{
  "Google": {
    "ClientId": "your-google-oauth-client-id"
  }
}
```

**Development Setup:**

```bash
dotnet user-secrets set "Google:ClientId" "123456789012-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com"
```

**Production Setup:**

```bash
export GOOGLE_CLIENT_ID="your-google-oauth-client-id"
```

## Authentication Flow

### Flow Types

MapMe supports four distinct authentication flows:

#### 1. Regular Registration Flow

**Process:**

1. User fills registration form
2. Server validates input and creates user account
3. Server creates default profile with provided data
4. Server returns JWT token with `IsNewUser: true`
5. Client redirects to `/profile` for profile completion

**Profile Prefilling:**

- Display name from registration form
- Email from registration form
- Personalized bio with user's name

#### 2. Google OAuth Registration Flow

**Process:**

1. User clicks "Sign up with Google"
2. Google OAuth popup authenticates user
3. Server checks if Google ID exists in database
4. **If new user:**
    - Creates user account with Google data
    - Creates default profile with Google information
    - Returns JWT token with `IsNewUser: true`
    - Client redirects to `/profile`
5. **If existing user:**
    - Updates last login time
    - Returns JWT token with `IsNewUser: false`
    - Client redirects to `/map`

**Profile Prefilling:**

- Display name from Google profile
- Email from Google account
- Profile picture from Google account (if available)
- Personalized bio: "Hello! I'm {DisplayName} and I'm new to MapMe..."
- Email verification automatically set to `true`

#### 3. Regular Login Flow

**Process:**

1. User enters username/password
2. Server validates credentials
3. Server returns JWT token with `IsNewUser: false`
4. Client redirects to `/map`

#### 4. Google OAuth Login Flow

**Process:**

1. User clicks "Sign in with Google"
2. Google OAuth popup authenticates user
3. Server finds existing user by Google ID
4. Server returns JWT token with `IsNewUser: false`
5. Client redirects to `/map`

### Authentication Sequence Diagram

```
User → Client → Server → Google → Database
 |       |        |        |        |
 |-------|--------|--------|--------| 1. Initiate Login
 |       |--------|        |        | 2. OAuth Request
 |       |        |--------|        | 3. Validate Token
 |       |        |        |--------| 4. Check/Create User
 |       |        |<-------|        | 5. User Data
 |       |<-------|        |        | 6. JWT Token + IsNewUser
 |<------|        |        |        | 7. Redirect Decision
```

## New User Detection and Profile Prefilling

### IsNewUser Flag

The `AuthenticationResponse` DTO includes an `IsNewUser` boolean flag for intelligent client-side routing:

```csharp
public record AuthenticationResponse(
    bool Success,
    string Message,
    AuthenticatedUser? User = null,
    string? Token = null,
    DateTimeOffset? ExpiresAt = null,
    bool IsNewUser = false  // New user detection flag
);
```

### Server-Side Logic

**New User Conditions:**

- Regular registration: Always `IsNewUser: true`
- Google OAuth: `IsNewUser: true` if Google ID not found in database
- Regular login: Always `IsNewUser: false`
- Existing Google user: Always `IsNewUser: false`

### Client-Side Redirect Logic

**Login.razor:**

```csharp
if (response.Success)
{
    if (response.IsNewUser)
    {
        // New user - redirect to profile for setup
        Navigation.NavigateTo("/profile", forceLoad: true);
    }
    else
    {
        // Existing user - redirect to main application
        Navigation.NavigateTo("/map", forceLoad: true);
    }
}
```

### Profile Prefilling Implementation

**Enhanced CreateInitialUserProfileAsync Method:**

```csharp
private async Task CreateInitialUserProfileAsync(string userId, string displayName, string email, string? pictureUrl = null)
{
    var sanitizedDisplayName = SanitizeUnicodeString(displayName);
    var sanitizedEmail = SanitizeUnicodeString(email);

    // Create personalized bio for new users
    var bio = string.IsNullOrEmpty(sanitizedEmail) || sanitizedEmail.Contains("@") 
        ? $"Hello! I'm {sanitizedDisplayName} and I'm new to MapMe. Looking forward to sharing my favorite places!"
        : "New MapMe user";

    // Create profile photos list with Google profile picture if available
    var photos = new List<UserPhoto>();
    if (!string.IsNullOrEmpty(pictureUrl))
    {
        photos.Add(new UserPhoto(
            Url: pictureUrl,
            IsPrimary: true
        ));
    }

    var defaultProfile = new UserProfile(
        Id: Guid.NewGuid().ToString(),
        UserId: userId,
        DisplayName: sanitizedDisplayName,
        Bio: bio,
        Photos: photos.AsReadOnly(),
        Preferences: new UserPreferences(
            Categories: new List<string>().AsReadOnly()
        ),
        Visibility: "public",
        CreatedAt: DateTimeOffset.UtcNow,
        UpdatedAt: DateTimeOffset.UtcNow
    );

    await _userProfileRepository.UpsertAsync(defaultProfile);
}
```

## Configuration

### Complete Configuration Example

```json
{
  "Jwt": {
    "SecretKey": "your-jwt-secret-key-minimum-32-characters",
    "ExpirationHours": 24,
    "Issuer": "MapMe-Server",
    "Audience": "MapMe-Client"
  },
  "Google": {
    "ClientId": "your-google-oauth-client-id"
  },
  "GoogleMaps": {
    "ApiKey": "your-google-maps-api-key"
  }
}
```

### Environment-Specific Setup

**Development (User Secrets):**
```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:SecretKey" "your-secure-jwt-secret-key-here"
dotnet user-secrets set "Google:ClientId" "your-google-oauth-client-id"
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-maps-api-key"
```

**Production (Environment Variables):**

```bash
export JWT_SECRET_KEY="your-secure-jwt-secret-key-here"
export GOOGLE_CLIENT_ID="your-google-oauth-client-id"
export GOOGLE_MAPS_API_KEY="your-google-maps-api-key"
```

## Security Considerations

### JWT Security

**Token Security:**

- **Strong Keys**: Minimum 32 characters for secret key
- **Secure Storage**: localStorage with proper handling
- **Token Expiration**: Configurable expiration times
- **Replay Protection**: Timestamp validation

**Validation:**

- **Signature Verification**: HMAC SHA256 validation on every request
- **Expiration Validation**: Automatic token expiry checking
- **Issuer/Audience Validation**: Proper token scope verification
- **Claims Validation**: User context verification

### Google OAuth Security

**Configuration Security:**

- **Exact URL Matching**: Origins and redirects must match exactly
- **HTTPS Only**: Never use HTTP for OAuth in production
- **Client ID Protection**: Treat as sensitive but not secret
- **Server-Side Validation**: Always validate Google tokens server-side

**Best Practices:**

- Separate OAuth clients for different environments
- Regular credential rotation
- OAuth usage monitoring
- Proper error handling for OAuth failures

### Data Sanitization

All user input is sanitized using `SanitizeUnicodeString()`:

- Removes problematic Unicode characters
- Prevents serialization issues
- Maintains data integrity
- Protects against injection attacks

## Testing and Validation

### Unit Test Coverage

**Authentication Service Tests:**

- New user registration with `IsNewUser: true`
- Existing user login with `IsNewUser: false`
- Google OAuth new user flow
- Google OAuth existing user flow
- Profile prefilling with personalized bio
- Duplicate email handling

### Integration Testing Scenarios

1. **Complete new user registration flow**
2. **Google OAuth new user flow with profile prefilling**
3. **Existing user login flow**
4. **Profile page access after new user creation**
5. **Map page access after existing user login**

### Manual Testing Checklist

- [ ] New user registration redirects to profile
- [ ] Profile page shows prefilled data
- [ ] Google sign-up creates personalized bio
- [ ] Existing user login goes to map
- [ ] Profile completion works correctly
- [ ] JWT tokens validate properly
- [ ] Token expiration handling works
- [ ] Google OAuth popup functions
- [ ] Error handling displays correctly

## Troubleshooting

### Common Issues

#### JWT Token Issues

**Problem**: `401 Unauthorized` responses
**Solutions:**

- Verify JWT secret key configuration
- Check token expiration
- Validate issuer/audience settings
- Ensure proper Authorization header format

**Problem**: Token validation failures
**Solutions:**

- Verify HMAC SHA256 signing
- Check system clock synchronization
- Validate token claims structure
- Ensure proper token storage/retrieval

#### Google OAuth Issues

**Problem**: `invalid_client` error
**Solutions:**

- Verify Google Client ID configuration
- Check OAuth client settings in Google Cloud Console
- Ensure API is enabled
- Verify environment variable setup

**Problem**: `redirect_uri_mismatch` error
**Solutions:**

- Exact match required for redirect URIs
- Include protocol (https://) and port
- Check development vs production URLs
- Update OAuth client configuration

#### Authentication Flow Issues

**Problem**: Infinite redirect loops
**Solutions:**

- Check `IsNewUser` flag logic
- Verify redirect URL configurations
- Ensure proper authentication state handling
- Check browser localStorage

**Problem**: Profile not prefilled
**Solutions:**

- Verify `CreateDefaultUserProfileAsync` execution
- Check Google user data retrieval
- Validate data sanitization process
- Ensure database upsert operations

### Debug Steps

1. **Check browser console** for JavaScript errors
2. **Verify network requests** in browser dev tools
3. **Check server logs** for authentication errors
4. **Validate configuration** values
5. **Test OAuth client** directly in Google Cloud Console
6. **Verify database** user and profile records

### Error Messages

| Error                        | Cause                       | Solution                      |
|------------------------------|-----------------------------|-------------------------------|
| `401: invalid_client`        | OAuth client not found      | Check Client ID configuration |
| `400: redirect_uri_mismatch` | Redirect URI not configured | Update OAuth client settings  |
| `JWT validation failed`      | Invalid token signature     | Verify JWT secret key         |
| `Token expired`              | Token past expiration       | Implement token refresh       |
| `User not found`             | Database lookup failed      | Check user creation process   |

## Production Deployment

### Environment Configuration

**Required Environment Variables:**

```bash
# JWT Configuration
JWT_SECRET_KEY="secure-production-jwt-secret-key-minimum-32-chars"
JWT_EXPIRATION_HOURS="24"
JWT_ISSUER="MapMe-Server"
JWT_AUDIENCE="MapMe-Client"

# Google OAuth
GOOGLE_CLIENT_ID="production-google-oauth-client-id"

# Database
COSMOS_DB_CONNECTION_STRING="production-cosmos-db-connection"
```

### Security Hardening

**Production Security Checklist:**

- [ ] Use strong JWT secret keys (minimum 32 characters)
- [ ] Enable HTTPS for all endpoints
- [ ] Configure proper CORS policies
- [ ] Implement rate limiting for auth endpoints
- [ ] Set up monitoring for failed authentication attempts
- [ ] Regular security audits of OAuth configuration
- [ ] Implement proper logging with secure data handling
- [ ] Use separate OAuth clients for different environments

### Monitoring and Logging

**Key Metrics to Monitor:**

- Authentication success/failure rates
- JWT token validation errors
- Google OAuth success/failure rates
- New user registration rates
- Profile prefilling success rates

**Secure Logging Examples:**

```csharp
// ✅ Good: Sanitized logging
_logger.LogInformation("User authentication successful: {UserId}", 
    SecureLogging.SanitizeUserIdForLog(userId));

// ✅ Good: OAuth event logging
_logger.LogInformation("Google OAuth login attempt: {Email}", 
    SecureLogging.SanitizeEmailForLog(email));

// ❌ Bad: Sensitive data logging
_logger.LogInformation("JWT token: {Token}", token); // Never log tokens
```

### Performance Considerations

**Optimization Strategies:**

- Cache JWT validation results
- Implement token refresh logic
- Use connection pooling for database operations
- Optimize Google OAuth token validation
- Implement proper async/await patterns

### Backup and Recovery

**Authentication Data Backup:**

- User accounts and credentials
- User profiles and preferences
- OAuth provider mappings
- JWT configuration and keys

**Recovery Procedures:**

- User account restoration
- Profile data recovery
- OAuth re-linking process
- JWT key rotation procedures

---

## Related Documentation

- [Cosmos DB Pruning](../backend/cosmos-db-pruning.md)
- [Security Overview](README.md)
- [Backend Configuration](../backend/configuration.md)
- [API Documentation](../api/README.md)

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-04  
**Maintainer**: MapMe Development Team
