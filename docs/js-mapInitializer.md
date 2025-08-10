# JavaScript Guide: mapInitializer.js

Location
- MapMe/MapMe/MapMe.Client/wwwroot/js/mapInitializer.js

Purpose
- Bootstraps Google Maps, renders custom marker overlays for user/place marks, builds grouped info windows, implements a photo lightbox, user profile popovers, and hover labels.

Key globals
- window.initMap(opts): initializes the map (called from Blazor once /config/maps provides an API key)
- window.MapMe.openPhotoViewer(urls, startIndex): opens the photo lightbox
- window.MapMe.debugRenderMockMarks(): renders mock date marks for local testing
- Optional hook: window.MapMe.getUserProfile(username): returns profile data for the popover

Primary flow
1) Initialize map and shared info window
2) Render marks → group by placeId or proximity (~25m)
3) For each group, create a transparent Marker + OverlayView container
4) Overlay contains:
   - Base place image (48px circle)
   - Up to 3 overlapping user chips + "+N" counter
   - Hidden hover label (shows place title on mouseenter)
5) Click overlay → open info window with:
   - Place photos strip
   - Per-user sections: name link with popover, messages, user photos
   - Click photo → open custom lightbox

Supported mark fields
- lat, lng, placeId (or place_id)
- title/placeName/name, address
- createdBy (user name)
- userPhotoUrl, userPhotoUrls[], userPhotos[]
- placePhotoUrl, placePhotoUrls[], placePhotos[]
- message/userMessage/note/comment/caption/description/text/msg

Grouping
- If placeId present, group on exact placeId
- Otherwise group by geographic proximity (Haversine) within ~25 meters

Info window content
- Title: place title if present; address below, muted
- Sections:
  - Place photos (if any)
  - For each user: heading with user link, a bulleted list of that user's messages (aggregated), and a horizontal strip of user photos
- Images are clickable and launch the lightbox at that section's index

Popover (user previews)
- Trigger: hover/click on user name in info window
- Loads profile details via getUserProfile(username):
  - Order: custom hook → /api/users/:username → mock
  - Displays: avatar, name, handle, bio, location, website, joinedAt, stats, interests, recentPhotos[], actions (View profile, Message)
- Caching: simple in-memory Map by lowercased username

Lightbox
- Minimal custom overlay: navigates via next/previous and closes on backdrop/click
- Esc closes, arrow keys navigate (if focus is within the document)

Styling
- Lightweight CSS injected for popover and lightbox
- Can be overridden by site CSS if needed

Performance & cleanup
- OverlayView onRemove cleans up DOM and Google listeners
- Map event listeners (bounds_changed, idle, zoom_changed, drag) re-draw positions
- Photo and user URL sets are deduplicated

Extensibility tips
- Add more fields to messages or profile mapping as backend evolves
- Replace the lightbox with a site-standard modal if desired
- Provide getUserProfile to integrate with your real user service

Troubleshooting
- If only one user photo appears, make sure to pass userPhotos[] or userPhotoUrls[]
- If no messages, verify the backend property names; we support several fallbacks
- If Maps doesn’t load, check /config/maps and referrer restrictions
