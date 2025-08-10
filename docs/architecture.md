# Architecture and Tech Stack

Overview
MapMe is a Blazor (interactive SSR + WebAssembly) application with a server host and a client project. It integrates Google Maps via JavaScript interop and custom overlay rendering for rich markers and info windows.

Projects
- MapMe/MapMe/MapMe (Server Host)
  - ASP.NET Core (.NET 10)
  - Serves Blazor pages and static assets
  - Provides configuration endpoints (e.g., /config/maps)
  - Recommended JSON library: System.Text.Json
- MapMe/MapMe/MapMe.Client (Blazor Client)
  - Components (Razor) and client-side assets
  - JS interop for Google Maps in wwwroot/js/mapInitializer.js

Runtime modes
- Interactive Server-Side Rendering (SSR): default dev experience
- WebAssembly (WASM): client components can execute in-browser where applicable

Core frontend modules
- mapInitializer.js: bootstraps Google Maps, renders custom marker overlays, groups marks by placeId or proximity, builds info windows, photo lightbox, user popovers, and hover labels.
- Map.razor: hosts the map surface, passes configuration and coordinates, wires JS interop.

Data model (frontend simplified)
- Mark
  - lat, lng, placeId (optional)
  - title/placeName/address (optional)
  - userPhotoUrl / userPhotoUrls / userPhotos (arrays supported)
  - placePhotoUrl / placePhotoUrls / placePhotos (arrays supported)
  - createdBy (string)
  - message/userMessage/note/comment (message-like fields supported)
- Grouping
  - Groups by placeId when present
  - Otherwise groups by proximity (~25m)

Google Maps integration
- Loads Maps JS via key provided from server (not hard-coded)
- Custom markers implemented with OverlayView stacked over a transparent Marker
- Info windows are custom HTML injected into a single google.maps.InfoWindow instance

Security & secrets
- No API keys committed to source
- Server reads key from configuration (User Secrets in Development, environment variable in Prod)

Extensibility points
- window.MapMe.getUserProfile(username): optional hook to supply rich profile data for the hover popover
- MapMe.debugRenderMockMarks(): helper to render test data for development

Performance considerations
- Overlay views are lightweight DOM nodes; avoid excessive reflows
- Photo URLs deduplicated via Sets
- Event listeners are cleaned up in onRemove

Known external endpoints
- GET /config/maps → returns Google Maps key
- GET /api/users/{username} → returns profile data (optional; only used if present)
