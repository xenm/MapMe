# Authentication & Authorization

## JWT Authentication

MapMe uses JWT (JSON Web Tokens) for stateless authentication across the application.

### Configuration

**Configuration Sources:**
- Primary: `appsettings.json` / User Secrets (Development)
- Fallback: Environment variables in dev/test to avoid placeholder misconfigurations

**Required Configuration:**
```json
{
  "Jwt": {
    "SecretKey": "your-jwt-secret-key-minimum-32-characters",
    "ExpirationHours": 24,
    "Issuer": "MapMe",
    "Audience": "MapMe-Users"
  }
}
```

**User Secrets Setup:**
```bash
dotnet user-secrets set "Jwt:SecretKey" "your-secure-jwt-secret-key-here"
```

### JWT Implementation

**Key Features:**
- HMAC SHA256 signing algorithm
- Configurable token expiration (default 24 hours)
- Claims-based user identification
- Automatic token refresh capabilities
- Stateless authentication (no server-side sessions)

**Token Claims:**
- `sub` (Subject): User ID
- `name`: Username
- `email`: User email address
- `iat` (Issued At): Token creation timestamp
- `exp` (Expiration): Token expiration timestamp

### Authentication Flow

1. **Login/Registration**: User provides credentials → Server validates → JWT token generated
2. **API Requests**: Client sends `Authorization: Bearer {token}` header
3. **Token Validation**: Server validates signature, expiration, and extracts user claims
4. **Token Refresh**: Automatic refresh when token nears expiration

### Security Considerations

**Token Security:**
- Strong key requirements (minimum 32 characters)
- Secure token storage in client (localStorage with proper handling)
- Token expiration and refresh mechanisms
- Protection against token replay attacks

**Validation:**
- Signature verification on every request
- Expiration time validation
- Issuer and audience validation
- Claims validation for user context

## Google OAuth Integration

**Configuration Required:**
```json
{
  "GoogleAuth": {
    "ClientId": "your-google-oauth-client-id"
  }
}
```

**OAuth Flow:**
1. Client initiates Google Sign-In
2. Google returns OAuth token
3. Server validates Google token
4. Server creates MapMe JWT token
5. Client uses MapMe JWT for subsequent requests

**Security Features:**
- Google token validation on server-side
- Automatic user profile creation/update
- Secure token exchange process

## Authorization Policies

**Endpoint Protection:**
- Anonymous endpoints: `/api/auth/*` (login, register, google-login)
- Protected endpoints: All other API endpoints require valid JWT
- Authorization middleware validates tokens automatically

**User Context:**
- `GetCurrentUserIdAsync()` extracts user ID from JWT claims
- User context available throughout request pipeline
- Proper error handling for invalid/expired tokens

## Test Environment Considerations

- Tests run with in-memory repositories by default
- Cosmos DB not required for unit/integration tests
- JWT config in tests may fall back to environment variables
- TestAuthenticationService provides mock authentication for testing

## Production Guidance

**Security Best Practices:**
- Fail fast on invalid JWT/configuration
- Use environment variables or secret stores for production
- Never commit real secrets to source control
- Implement proper HTTPS and security headers
- Regular secret key rotation

**Health Checks:**
- Validate JWT configuration at startup
- Monitor authentication success/failure rates
- Alert on unusual authentication patterns

---

**Related Documentation:**
- [Security Overview](README.md)
- [Data Protection](data-protection.md)
- [Secure Logging](secure-logging.md)
- [Backend Configuration](../backend/configuration.md)
