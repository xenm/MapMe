# Manual Testing Scenarios

This guide describes end-to-end scenarios to validate the data flow for profiles and date marks.

Prerequisites
- Run the server: `dotnet run --project MapMe/MapMe`
- Base URL: https://localhost:8008 (or the URL printed in console)

1) Create a profile
- Request
```
curl -k -X POST https://localhost:8008/api/profiles \
  -H "Content-Type: application/json" \
  -d '{
    "id": "profile_alex",
    "userId": "user-alex-123",
    "displayName": "Alex",
    "bio": "Traveler",
    "photos": [{"url":"https://pics/1.jpg","isPrimary":true}],
    "preferredCategories": ["coffee","art"],
    "visibility": "public"
  }'
```
- Verify: 201 Created and response payload matches.
- Fetch:
```
curl -k https://localhost:8008/api/profiles/profile_alex
```

2) Add date marks
```
# Mark 1
curl -k -X POST https://localhost:8008/api/datemarks \
  -H "Content-Type: application/json" \
  -d '{
    "id":"dm1","userId":"user-alex-123",
    "latitude":37.7765,"longitude":-122.4167,
    "placeId":"ChIJ-BlueBottle","placeName":"Blue Bottle Coffee",
    "placeTypes":["cafe","food"],
    "placeRating":4.4,"placePriceLevel":2,
    "address":"300 Webster St","city":"Oakland","country":"US",
    "categories":["coffee","date"],
    "tags":["first-date","cozy"],
    "qualities":["romantic"],
    "notes":"Great vibes!",
    "visitDate":"2025-08-09",
    "visibility":"public"
  }'

# Mark 2
curl -k -X POST https://localhost:8008/api/datemarks \
  -H "Content-Type: application/json" \
  -d '{
    "id":"dm2","userId":"user-alex-123",
    "latitude":37.7790,"longitude":-122.4140,
    "placeName":"Asian Art Museum",
    "placeTypes":["museum","art"],
    "categories":["art"],
    "tags":["exhibit"],
    "qualities":["indoors"],
    "visitDate":"2025-08-10",
    "visibility":"public"
  }'
```

3) Query all marks for user
```
curl -k "https://localhost:8008/api/users/user-alex-123/datemarks"
```
- Expect both dm1 and dm2 sorted by createdAt desc.

4) Filter by categories/tags/qualities and date window
```
# Only coffee
curl -k "https://localhost:8008/api/users/user-alex-123/datemarks?categories=coffee"

# Tags include cozy
curl -k "https://localhost:8008/api/users/user-alex-123/datemarks?tags=cozy"

# Date window
curl -k "https://localhost:8008/api/users/user-alex-123/datemarks?from=2025-08-09&to=2025-08-09"
```

5) Map viewport (prototype placeholder)
```
# Prototype endpoint currently returns empty; to be replaced with Cosmos Geo container
curl -k "https://localhost:8008/api/map/datemarks?lat=37.776&lng=-122.417&radiusMeters=500&categories=coffee"
```

Cosmos DB configuration (optional for now)
- Add to User Secrets:
```
dotnet user-secrets --project MapMe/MapMe set "Cosmos:Endpoint" "https://<your-account>.documents.azure.com:443/"
dotnet user-secrets --project MapMe/MapMe set "Cosmos:Key" "<your-key>"
dotnet user-secrets --project MapMe/MapMe set "Cosmos:Database" "mapme"
```
- With these set, the app will switch to Cosmos repositories automatically.
