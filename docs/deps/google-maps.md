# Google Maps (Dependency Setup)

Configure Google Maps JavaScript API for MapMe.

## Prerequisites
- Google Cloud project
- Enable APIs:
  - Maps JavaScript API
  - Places API
  - Geocoding API

## API Key
Use User Secrets for development; env vars or secret store for production.

```bash
cd MapMe/MapMe/MapMe
dotnet user-secrets init
# Google Maps API Key
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-maps-api-key"
```

Production:
```bash
export GOOGLE_MAPS_API_KEY="your-google-maps-api-key"
```

Key lookup order:
1. Configuration (including User Secrets): `GoogleMaps:ApiKey`
2. Environment variable: `GOOGLE_MAPS_API_KEY`

## Security
- Restrict key to your domain/localhost
- Restrict to only Maps JS, Places, Geocoding APIs
- Never commit keys

## Verification
- Start the app and ensure the map loads on `/`
- Check browser console for Maps load errors

