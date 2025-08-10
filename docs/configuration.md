# Configuration and Secrets

Google Maps API Key
- Source of truth is server configuration.
- Lookup order:
  1) Configuration key: GoogleMaps:ApiKey (includes User Secrets in Development)
  2) Environment variable: GOOGLE_MAPS_API_KEY

Local development (User Secrets)
- From MapMe/MapMe/MapMe:
  - dotnet user-secrets init
  - dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-api-key"

Environment variables
- Set GOOGLE_MAPS_API_KEY in your shell or container environment.

Server endpoint
- GET /config/maps → returns the configured API key to the client at runtime.

Notes
- Do not commit secrets to source control or launchSettings.json.
- Restrict the key in Google Cloud Console (HTTP referrers, API restrictions).
