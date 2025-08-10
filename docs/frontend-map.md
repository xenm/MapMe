# Frontend: Maps, Markers, Info Windows

Overview
- Google Maps is initialized from mapInitializer.js via initMap, after fetching the API key from /config/maps.
- Marks are grouped by placeId or proximity (~25m) and rendered as custom overlays.
- Info windows show a top strip of place photos, then per-user sections (name link with popover, messages, user photos).
- Photos open in a custom lightbox viewer.
- Hovering a marker shows a label above the icon preferring the place title.

Key behaviors
- Grouping logic: prefers placeId; otherwise clusters by geographic distance.
- Marker overlay: place image (48px circle) + up to 3 overlapping user chips + "+N" counter.
- Info window: scrollable content; per-user messages aggregated from several fields; click thumbnails to open viewer.
- User name popover: shows profile details (hook → API → mock) and recent photos.

Supported fields (marks)
- Coordinates: lat, lng
- Identity: createdBy
- Place: placeId, title/placeName/name, address
- Photos (user): userPhotoUrl, userPhotoUrls[], userPhotos[]
- Photos (place): placePhotoUrl, placePhotoUrls[], placePhotos[]
- Messages: message, userMessage, note, comment, caption, description, text, msg

Extensibility
- window.MapMe.getUserProfile(username): provide profile data to the popover including recentPhotos[].
- Styling: see injected styles in mapInitializer.js for popover and lightbox; override via your CSS if desired.

Troubleshooting
- Missing messages: ensure the backend uses one of the supported message fields or map it before rendering.
- Single photo only: ensure arrays (userPhotos/placePhotos) are passed; we merge both *Url* and *Photos* arrays.
