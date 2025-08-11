# MapMe

A Blazor app (interactive SSR + WebAssembly) with Google Maps integration via JS interop.

## Quick start

- Prerequisites: .NET 10 SDK, Node not required.
- Run (server project):
  - Rider/VS: run the "MapMe" project
  - CLI: `dotnet run --project MapMe/MapMe/MapMe.csproj`
- Dev port (via launchSettings.json):
  - HTTPS only: https://localhost:8008

## Configuration

We do not commit secrets. The client fetches the Google Maps key from the server at `/config/maps`.

Server key lookup order (effective):
1) `GoogleMaps:ApiKey` from configuration (includes User Secrets in Development)
2) `GOOGLE_MAPS_API_KEY` environment variable

Recommended:
- Development: User Secrets
  - From `MapMe/MapMe/MapMe`:
    - `dotnet user-secrets init`
    - `dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-api-key"`
- CI/Prod/Containers: Environment variable
  - `GOOGLE_MAPS_API_KEY=your-google-api-key`

You can also set env vars in Rider Run Configuration (Edit Configurations… → Environment).

## Google Maps integration

- JS file: `MapMe/MapMe/MapMe.Client/wwwroot/js/mapInitializer.js`
  - No API keys in source. The key is passed from Blazor to `initMap` after fetching `/config/maps`.
- Blazor page: `MapMe/MapMe/MapMe.Client/Pages/Map.razor`
  - Handles searching and centers the map without re-initializing.

## Security notes

- Do not commit real API keys to source or to `launchSettings.json`.
- Restrict your Google Maps key in Google Cloud Console (HTTP referrers/origins and API restrictions).

## Troubleshooting

- Port in use: another dev process may be bound to 5260/7160. Kill it or change the profile URLs.
- `HttpClient` injection error in WASM/SSR:
  - Client: `MapMe.Client/Program.cs` registers `HttpClient` with BaseAddress.
  - Server: `MapMe/Program.cs` registers `HttpClient` for SSR and adds `IHttpContextAccessor`.

## Project layout

- `MapMe/MapMe/MapMe` — server (host) project
- `MapMe/MapMe/MapMe.Client` — Blazor client project

## Data flow overview

- Entities
  - UserProfile: profile metadata, preferences, photos
  - DateMark: user’s saved place/date with geo, categories/tags/qualities, notes
- Storage (phased)
  - Phase 1: In-memory repositories for local development
  - Phase 2: Azure Cosmos DB
    - Users (PK: /id)
    - DateMarksByUser (PK: /userId)
    - DateMarksGeo (PK: /geoHashPrefix) via Change Feed projection
- Search
  - Structured filters (categories/tags/qualities/time/geo): Cosmos
  - Full-text (notes/place name): Azure AI Search (planned)

## API endpoints

- Profiles
  - POST /api/profiles
  - GET /api/profiles/{id}
- Date marks
  - POST /api/datemarks
  - GET /api/users/{userId}/datemarks?from&to&categories[]&tags[]&qualities[]
- Map (prototype)
  - GET /api/map/datemarks?lat&lng&radiusMeters&categories[]&tags[]&qualities[]

See docs/manual-testing.md for sample curl commands.

## Cosmos DB configuration

Provide these settings to switch from in-memory to Cosmos repositories:

```
Cosmos:Endpoint = https://<your-account>.documents.azure.com:443/
Cosmos:Key = <your-key>
Cosmos:Database = mapme
```

Recommended: use User Secrets in Development

```
dotnet user-secrets --project MapMe/MapMe/MapMe set "Cosmos:Endpoint" "https://..."
dotnet user-secrets --project MapMe/MapMe/MapMe set "Cosmos:Key" "..."
dotnet user-secrets --project MapMe/MapMe/MapMe set "Cosmos:Database" "mapme"
```

## Testing

- Manual testing scenarios: docs/manual-testing.md
- Automated tests (xUnit): MapMe.Tests project
  - Unit: normalization utilities, repositories
  - Service-level: minimal API endpoints via WebApplicationFactory

Run tests:

```
dotnet test MapMe.sln -v minimal
```

## .NET and JSON

- Target framework: .NET 10
- Serialization: System.Text.Json

