# Google OAuth Integration

## Overview

MapMe integrates with Google OAuth 2.0 for secure user authentication, allowing users to sign in with their Google accounts.

## Prerequisites

### Google Cloud Project Setup
- Create or use existing Google Cloud project
- Enable billing for the project
- Enable required APIs:
  - **Google Identity Services API** - OAuth authentication

## OAuth 2.0 Client ID Setup

### Create OAuth Client ID
1. Open [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project
3. Navigate to **APIs & Services → Credentials**
4. Click **Create Credentials → OAuth client ID**
5. Select **Web application**
6. Configure the client:
   - **Name**: MapMe Development (or appropriate environment name)
   - **Authorized JavaScript origins**:
     - `https://localhost:8008` (development)
     - `https://yourdomain.com` (production)
   - **Authorized redirect URIs**:
     - `https://localhost:8008/signin-google` (development)
     - `https://yourdomain.com/signin-google` (production)

### Environment-Specific Configuration

**Development Setup:**
```bash
cd MapMe/MapMe/MapMe
dotnet user-secrets init
dotnet user-secrets set "Google:ClientId" "123456789012-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com"
```

**Production Setup:**
```bash
export GOOGLE_CLIENT_ID="123456789012-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com"
```

**Configuration Structure:**
```json
{
  "Google": {
    "ClientId": "your-google-oauth-client-id"
  }
}
```

## Implementation Details

### Server-Side Configuration
The OAuth client ID is served to the client via the `/config/google-client-id` endpoint:
```csharp
app.MapGet("/config/google-client-id", (IConfiguration config) =>
{
    var clientId = config["Google:ClientId"] ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    return Results.Ok(new { clientId });
});
```

### Client-Side Integration
The client fetches the OAuth client ID and initializes Google Sign-In:
```javascript
// Fetch OAuth client ID from server
const response = await fetch('/config/google-client-id');
const config = await response.json();

// Initialize Google Sign-In
google.accounts.id.initialize({
    client_id: config.clientId,
    callback: handleGoogleSignIn
});
```

## Authentication Flow

### Sign-In Process
1. User clicks "Sign in with Google" button
2. Google OAuth popup/redirect flow initiated
3. User authenticates with Google
4. Google returns OAuth token to MapMe
5. MapMe validates token with Google
6. MapMe creates/updates user profile
7. MapMe generates JWT token for session
8. User is authenticated in MapMe application

### Token Validation
```csharp
// Server-side Google token validation
public async Task<GoogleUserInfo> ValidateGoogleTokenAsync(string googleToken)
{
    using var httpClient = new HttpClient();
    var response = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={googleToken}");
    
    if (response.IsSuccessStatusCode)
    {
        var tokenInfo = await response.Content.ReadFromJsonAsync<GoogleTokenInfo>();
        return new GoogleUserInfo
        {
            Id = tokenInfo.UserId,
            Email = tokenInfo.Email,
            Name = tokenInfo.Name
        };
    }
    
    return null;
}
```

## Security Considerations

### OAuth Configuration Security
- **Origins and Redirects**: Ensure exact match with configured URLs
- **HTTPS Only**: Never use HTTP for OAuth in production
- **Client ID Protection**: Treat as sensitive but not secret (visible in client)
- **Token Validation**: Always validate Google tokens server-side

### Best Practices
- Use different OAuth clients for different environments
- Regularly review and rotate OAuth credentials
- Monitor OAuth usage and failed attempts
- Implement proper error handling for OAuth failures

## Error Handling

### Common OAuth Errors
- **invalid_client**: Client ID not found or misconfigured
- **redirect_uri_mismatch**: Redirect URI doesn't match configured values
- **access_denied**: User denied permission
- **popup_blocked**: Browser blocked OAuth popup

### Error Handling Implementation
```javascript
function handleGoogleSignInError(error) {
    console.error('Google Sign-In error:', error);
    
    switch (error.error) {
        case 'popup_blocked':
            showErrorMessage('Please allow popups for Google Sign-In');
            break;
        case 'access_denied':
            showErrorMessage('Google Sign-In was cancelled');
            break;
        default:
            showErrorMessage('Google Sign-In failed. Please try again.');
    }
}
```

## Testing and Verification

### Development Verification
1. Start the MapMe application
2. Navigate to `https://localhost:8008/config/google-client-id`
3. Verify the endpoint returns your configured Client ID
4. Test Google Sign-In flow on login page
5. Check browser console for OAuth errors

### Production Verification
- Test OAuth flow with production domain
- Verify SSL certificate validity
- Monitor OAuth success/failure rates
- Test with different browsers and devices

## Troubleshooting

### Common Issues
- **Origins/Redirect URIs**: Must exactly match configured values (including HTTPS and port)
- **Browser Cache**: Clear cache if changes don't appear
- **API Enablement**: Verify Google Identity Services API is enabled
- **SSL Certificates**: Ensure valid HTTPS certificates

### Debug Steps
1. Check browser console for JavaScript errors
2. Verify OAuth client ID in network requests
3. Test OAuth client directly in Google Cloud Console
4. Check server logs for OAuth validation errors
5. Verify API quotas and billing status

### Error Messages
- **Error 401: invalid_client**: OAuth client not found - check Client ID configuration
- **Error 400: redirect_uri_mismatch**: Redirect URI not configured - update OAuth client settings
- **Network errors**: Check HTTPS configuration and firewall settings

## Production Deployment

### Environment Configuration
- Use environment variables or secret management for Client ID
- Configure separate OAuth clients for staging and production
- Set up monitoring for OAuth success/failure rates
- Implement proper logging for OAuth events (using secure logging practices)

### Security Hardening
- Implement rate limiting for OAuth endpoints
- Add CSRF protection for OAuth flows
- Monitor for suspicious OAuth activity
- Regular security audits of OAuth configuration

---

**Related Documentation:**
- [Security Overview](README.md)
- [Authentication](authentication.md)
- [Secure Logging](secure-logging.md)
- [Backend Configuration](../backend/configuration.md)
