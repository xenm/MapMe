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

