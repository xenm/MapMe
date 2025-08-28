# Google OAuth Setup for MapMe

## Issue
You're getting `Error 401: invalid_client` because MapMe is using placeholder Google OAuth credentials instead of real ones.

## Root Cause
- Your `appsettings.Development.json` likely has `"ClientId": "YOUR_GOOGLE_CLIENT_ID_HERE"` (placeholder)
- The JavaScript `googleAuth.js` falls back to the same placeholder when it can't get a real client ID
- Google OAuth requires actual credentials from Google Cloud Console

## Solution Steps

### 1. Create Google OAuth 2.0 Credentials

1. **Go to Google Cloud Console**: https://console.cloud.google.com/
2. **Create or select a project** for your MapMe application
3. **Enable required APIs**:
   - Go to "APIs & Services" > "Library"
   - Search for and enable:
     - "Google Identity Services API"
     - "Google+ API" (if available)
4. **Create OAuth 2.0 credentials**:
   - Go to "APIs & Services" > "Credentials"
   - Click "Create Credentials" > "OAuth 2.0 Client IDs"
   - Choose "Web application"
   - Set name: "MapMe Development"

### 2. Configure Authorized Origins and Redirect URIs

**Authorized JavaScript origins**:
```
https://localhost:8008
```

**Authorized redirect URIs**:
```
https://localhost:8008/signin-google
```

### 3. Update Your Configuration

After creating credentials, copy your **Client ID** and update your `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Azure.Cosmos": "Information"
    }
  },
  "AllowedHosts": "*",
  "Cosmos": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "Database": "mapme"
  },
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY_HERE"
  },
  "Google": {
    "ClientId": "123456789012-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com"
  }
}
```

**Replace the ClientId value with your actual Google Client ID from step 2.**

### 4. Test the Fix

1. **Restart your MapMe application**:
   ```bash
   cd /Users/adamzaplatilek/GitHub/MapMe/MapMe/MapMe
   dotnet run
   ```

2. **Navigate to the login page**: http://localhost:8008/login

3. **Click "Continue with Google"** - it should now work without the 401 error

### 5. Verify Configuration Endpoint

Your MapMe app exposes the Google Client ID at: http://localhost:8008/config/google-client-id

You can test this endpoint to verify it's returning your real Client ID instead of the placeholder.

## Security Notes

- Never commit your real Google Client ID to version control
- The `appsettings.Development.json` file is already gitignored for security
- For production, use environment variables or Azure Key Vault
- Keep your Client Secret secure (though MapMe uses client-side OAuth which doesn't require the secret)

## Troubleshooting

**If you still get 401 errors:**
1. Verify the Client ID is correctly copied (no extra spaces/characters)
2. Check that your localhost URLs match the authorized origins exactly
3. Make sure the Google Identity Services API is enabled
4. Try clearing browser cache and cookies
5. Check browser developer console for additional error details

**Common mistakes:**
- Using Client Secret instead of Client ID
- Mismatched redirect URIs (http vs https, port numbers)
- Not enabling required Google APIs
- Typos in the configuration file JSON syntax
