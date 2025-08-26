# JWT Authentication Implementation Guide

## Overview

MapMe uses JSON Web Tokens (JWT) for stateless, secure authentication. This document provides comprehensive guidance on the JWT implementation, security considerations, and operational procedures.

## Architecture

### JWT Token Flow
```
1. User Login/Register → Server validates credentials
2. Server generates JWT token with user claims
3. Client stores token in localStorage
4. Client sends Authorization: Bearer {token} header
5. Server validates token and extracts user identity
6. API request processed with authenticated user context
```

### Key Components

#### Backend Components
- **`IJwtService`**: Interface for JWT operations
- **`JwtService`**: Core JWT implementation with token generation/validation
- **`JwtAuthenticationHandler`**: Custom authentication handler for automatic token validation
- **Authentication Endpoints**: `/api/auth/login`, `/api/auth/register`, `/api/auth/validate-token`

#### Client Components
- **`AuthenticationService`**: Client-side JWT token management
- **Token Storage**: Secure localStorage management with key `mapme_jwt_token`
- **HTTP Interceptor**: Automatic Authorization header injection

## Configuration

### Server Configuration (appsettings.json)
```json
{
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here-must-be-at-least-32-characters",
    "Issuer": "MapMe",
    "Audience": "MapMe-Users",
    "ExpiryMinutes": 1440
  }
}
```

### Environment Variables (Production)
```bash
JWT__SECRETKEY=your-production-secret-key
JWT__ISSUER=MapMe-Production
JWT__AUDIENCE=MapMe-Production-Users
JWT__EXPIRYMINUTES=1440
```

## Security Best Practices

### Token Security
- **Secret Key**: Use cryptographically secure 256-bit keys
- **Token Expiration**: Default 24 hours, configurable per environment
- **HTTPS Only**: All JWT tokens must be transmitted over HTTPS
- **No Sensitive Data**: Never include passwords or sensitive data in JWT payload

### Storage Security
- **Client Storage**: localStorage with secure key naming
- **Server-Side**: Stateless - no server-side token storage required
- **Token Rotation**: Implement refresh token mechanism for long-lived sessions

### Validation Security
- **Signature Verification**: HMAC SHA256 signature validation
- **Expiration Check**: Automatic token expiration enforcement
- **Issuer/Audience**: Validate token issuer and audience claims
- **Claims Validation**: Verify required claims (UserId, Username)

## API Endpoints

### Authentication Endpoints

#### POST /api/auth/login
**Request:**
```json
{
  "username": "user@example.com",
  "password": "SecurePassword123!",
  "rememberMe": false
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-08-27T20:57:34Z",
  "user": {
    "userId": "user-123",
    "username": "user@example.com",
    "displayName": "John Doe"
  }
}
```

#### POST /api/auth/register
**Request:**
```json
{
  "username": "newuser@example.com",
  "email": "newuser@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "displayName": "New User"
}
```

**Response:** Same as login response

#### GET /api/auth/validate-token
**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:**
```json
{
  "userId": "user-123",
  "username": "user@example.com",
  "displayName": "John Doe",
  "isValid": true,
  "expiresAt": "2025-08-27T20:57:34Z"
}
```

### Protected Endpoints
All API endpoints except authentication endpoints require JWT token:
```
Authorization: Bearer {jwt-token}
```

## Client-Side Implementation

### Token Management
```typescript
// Store token
localStorage.setItem('mapme_jwt_token', token);

// Retrieve token
const token = localStorage.getItem('mapme_jwt_token');

// Clear token (logout)
localStorage.removeItem('mapme_jwt_token');
```

### HTTP Request Headers
```typescript
// Automatic header injection
headers: {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
}
```

### Token Validation
```typescript
// Validate token before API calls
const isValid = await authService.validateTokenAsync();
if (!isValid) {
  // Redirect to login
  navigation.navigateTo('/login');
}
```

## Error Handling

### Common Error Responses

#### 401 Unauthorized
```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired token",
  "details": "Token validation failed"
}
```

#### 403 Forbidden
```json
{
  "error": "Forbidden",
  "message": "Insufficient permissions",
  "details": "User does not have required permissions"
}
```

### Client-Side Error Handling
```csharp
try 
{
    var response = await httpClient.GetAsync("/api/protected-endpoint");
    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        // Token expired or invalid - redirect to login
        await authService.LogoutAsync();
        navigation.NavigateTo("/login");
    }
}
catch (HttpRequestException ex)
{
    // Handle network errors
    logger.LogError(ex, "Network error during API request");
}
```

## Testing

### Unit Testing
```csharp
[Fact]
public async Task GenerateToken_ValidUser_ReturnsValidToken()
{
    // Arrange
    var jwtService = new JwtService(mockConfiguration.Object, mockLogger.Object);
    var user = new User { UserId = "test-123", Username = "test@example.com" };
    
    // Act
    var (token, expiresAt) = jwtService.GenerateToken(user);
    
    // Assert
    token.Should().NotBeNullOrEmpty();
    expiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
}
```

### Integration Testing
```csharp
[Fact]
public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
{
    // Arrange
    var token = await CreateTestUserAndGetTokenAsync();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    // Act
    var response = await client.GetAsync("/api/protected-endpoint");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Monitoring and Logging

### Key Metrics to Monitor
- **Token Generation Rate**: Monitor for unusual spikes
- **Token Validation Failures**: Track invalid/expired tokens
- **Authentication Failures**: Monitor failed login attempts
- **Token Expiration Patterns**: Analyze token usage patterns

### Security Alerts
- **Brute Force Attempts**: Multiple failed logins from same IP
- **Token Replay Attacks**: Same token used from multiple IPs
- **Unusual Token Patterns**: Tokens with unexpected claims
- **High Failure Rates**: Sudden increase in authentication failures

## Troubleshooting

### Common Issues

#### "Invalid token" Errors
1. Check token expiration
2. Verify secret key configuration
3. Ensure HTTPS is used
4. Validate token format

#### "Token not found" Errors
1. Check localStorage for token
2. Verify token storage key name
3. Check for token clearing on logout

#### Performance Issues
1. Monitor token validation frequency
2. Check for unnecessary token regeneration
3. Optimize token payload size

### Debug Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "MapMe.Services.JwtService": "Debug",
      "MapMe.Authentication": "Debug"
    }
  }
}
```

## Production Deployment

### Security Checklist
- [ ] Use strong, unique secret keys per environment
- [ ] Enable HTTPS everywhere
- [ ] Configure appropriate token expiration
- [ ] Set up monitoring and alerting
- [ ] Implement rate limiting on auth endpoints
- [ ] Regular security audits

### Performance Considerations
- **Token Size**: Keep JWT payload minimal
- **Validation Caching**: Consider caching validation results
- **Secret Key Rotation**: Plan for periodic key rotation
- **Load Balancing**: Ensure stateless operation across instances

## Migration Notes

### From Session-Based Authentication
The migration from session-based to JWT authentication included:
- Replaced `ISessionRepository` with `IJwtService`
- Updated all DTOs to use `Token` instead of `SessionId`
- Modified client-side storage from session IDs to JWT tokens
- Updated all HTTP headers from `X-Session-Id` to `Authorization: Bearer`
- Migrated all tests to use JWT authentication patterns

### Breaking Changes
- Client applications must update to use JWT tokens
- API clients must send Authorization headers instead of session headers
- Token format and validation logic completely changed

## Support and Maintenance

### Regular Tasks
- Monitor token expiration patterns
- Review authentication logs
- Update secret keys periodically
- Performance optimization reviews

### Emergency Procedures
- **Compromised Secret Key**: Immediate key rotation procedure
- **Mass Token Invalidation**: Force logout all users
- **Security Incident Response**: Authentication system lockdown

For additional support, refer to the MapMe development team or security team.
