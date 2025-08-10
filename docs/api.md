# Server API Overview

Status: minimal; expand as backend features grow.

Configuration
- GET /config/maps
  - Returns: { apiKey: string }
  - Purpose: Supplies Google Maps API key to the client at runtime.

Users (optional; used by popovers if available)
- GET /api/users/{username}
  - Returns (example shape):
    {
      "fullName": "Jane Doe",
      "username": "janedoe",
      "bio": "Traveler",
      "location": "Prague",
      "website": "https://example.com",
      "joinedAt": "2024-01-02",
      "followersCount": 120,
      "followingCount": 75,
      "photosCount": 34,
      "interests": ["coffee", "parks"],
      "avatar": "https://.../avatar.jpg",
      "recentPhotos": ["https://.../1.jpg", "https://.../2.jpg"]
    }

Notes
- The client will map similar field names (name/fullName, createdAt/joinedAt, url/website, etc.).
- If this endpoint is not present, the popover falls back to mock data.

Future endpoints (suggested)
- GET /api/marks?bbox=... — Retrieve marks for the current viewport
- POST /api/marks — Create a new mark
- GET /api/places/{placeId} — Retrieve place details and photos
- GET /api/users/{username}/marks — Fetch marks by user
