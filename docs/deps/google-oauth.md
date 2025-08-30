# Google OAuth (Dependency Setup)

Use this guide to configure Google OAuth for MapMe in Development and Production.

## Prerequisites
- Google Cloud project
- Enabled APIs:
  - Google Identity Services API

## Create OAuth 2.0 Client ID (Web)
1. Open https://console.cloud.google.com/
2. Select your project
3. APIs & Services → Credentials → Create Credentials → OAuth client ID → Web
4. Name: MapMe Development
5. Authorized JavaScript origins:
   - https://localhost:8008
6. Authorized redirect URIs:
   - https://localhost:8008/signin-google

## Configure MapMe
Prefer User Secrets for development.

```bash
cd MapMe/MapMe/MapMe
dotnet user-secrets init
# Google Client ID
dotnet user-secrets set "Google:ClientId" "123456789012-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com"
# Google Maps API Key (separate)
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-maps-api-key"
```

Production: use environment variables or a secret store.

## Verify
- Start app and open https://localhost:8008/config/google-client-id
- The endpoint should return your configured ClientId (never commit it to Git)

## Troubleshooting
- Ensure origins/redirect URIs exactly match (https and port)
- Clear browser cache if changes don’t appear
- Verify Google Identity Services API is enabled
- Check browser console and server logs for details

