# Google Maps Integration

## Overview

MapMe integrates with Google Maps JavaScript API to provide interactive mapping functionality, place search, and location services.

## Prerequisites

### Google Cloud Project Setup
- Create or use existing Google Cloud project
- Enable billing for the project
- Enable required APIs:
  - **Maps JavaScript API** - Core mapping functionality
  - **Places API** - Place search and details
  - **Geocoding API** - Address to coordinates conversion

### API Key Configuration

**Development Setup:**
```bash
cd MapMe/MapMe/MapMe
dotnet user-secrets init
# Replace empty string with your actual Google Maps API key
dotnet user-secrets set "GoogleMaps:ApiKey" ""
```

**Production Setup:**
```bash
# Replace empty string with your actual Google Maps API key
export GOOGLE_MAPS_API_KEY=""
```

**Configuration Lookup Order:**
1. Configuration (including User Secrets): `GoogleMaps:ApiKey`
2. Environment variable: `GOOGLE_MAPS_API_KEY`

## API Key Security

### Key Restrictions
- **HTTP referrers**: Restrict to your domain and localhost
  - `localhost:*`
  - `*.example.com` (replace with your actual domain)
- **API restrictions**: Limit to only required APIs
  - Maps JavaScript API
  - Places API
  - Geocoding API

### Security Best Practices
- Never commit API keys to source control
- Use different keys for development and production
- Set up billing alerts and quotas
- Regularly rotate API keys
- Monitor API usage for anomalies

## Implementation Details

### Server-Side Configuration
The API key is served to the client via the `/config/maps` endpoint:
```csharp
app.MapGet("/config/maps", (IConfiguration config) =>
{
    var apiKey = config["GoogleMaps:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
    return Results.Ok(new { apiKey });
});
```

**Note**: This endpoint serves the API key to authenticated clients only. The key is configured via User Secrets (development) or environment variables (production) and never hardcoded in source code.

### Client-Side Integration
The client fetches the API key at runtime and initializes the Google Maps JavaScript API:
```javascript
// Fetch API key from server configuration endpoint
const response = await fetch('/config/maps');
const config = await response.json();

// Initialize Google Maps with dynamically retrieved API key
const script = document.createElement('script');
script.src = `https://maps.googleapis.com/maps/api/js?key=${config.apiKey}&libraries=places&callback=initMap`;
document.head.appendChild(script);
```

**Security Note**: The API key is retrieved dynamically from the server configuration, never embedded in client-side code or committed to source control.

## Features Implemented

### Core Mapping
- Interactive map display
- Custom markers for Date Marks
- Marker clustering for performance
- Map controls and navigation

### Places Integration
- Place search functionality
- Place details retrieval
- Place photos and information
- Autocomplete for location input

### User Interactions
- Click to create Date Marks
- Drag markers to update locations
- Info windows with place details
- User profile popups on markers

## API Usage Patterns

### Place Search
```javascript
const service = new google.maps.places.PlacesService(map);
service.findPlaceFromQuery({
    query: searchText,
    fields: ['place_id', 'name', 'geometry', 'photos', 'formatted_address']
}, (results, status) => {
    if (status === google.maps.places.PlacesServiceStatus.OK) {
        // Handle results
    }
});
```

### Place Details
```javascript
service.getDetails({
    placeId: placeId,
    fields: ['name', 'rating', 'formatted_phone_number', 'geometry', 'photos', 'url']
}, (place, status) => {
    if (status === google.maps.places.PlacesServiceStatus.OK) {
        // Handle place details
    }
});
```

## Error Handling

### Common Issues
- **API key not configured**: Check User Secrets and environment variables
- **API key restrictions**: Verify domain and API restrictions
- **Quota exceeded**: Monitor usage and increase quotas if needed
- **Network issues**: Implement retry logic for API calls

### Error Responses
```javascript
// Handle API errors
if (status !== google.maps.places.PlacesServiceStatus.OK) {
    console.error('Places API error:', status);
    // Show user-friendly error message
    showErrorMessage('Unable to load place information. Please try again.');
}
```

## Performance Optimization

### Best Practices
- Use marker clustering for large numbers of markers
- Implement lazy loading for place photos
- Cache place details to reduce API calls
- Use appropriate zoom levels to minimize tile requests

### Quota Management
- Monitor daily API usage
- Implement client-side caching
- Use efficient API field selections
- Consider implementing rate limiting

## Verification and Testing

### Development Verification
1. Start the MapMe application
2. Navigate to the map page
3. Verify map loads without errors
4. Check browser console for API errors
5. Test place search functionality
6. Verify marker creation and interaction

### Production Monitoring
- Set up API usage monitoring
- Configure billing alerts
- Monitor error rates and types
- Track performance metrics

## Troubleshooting

### Common Problems
- **Map not loading**: Check API key configuration and restrictions
- **Places not found**: Verify Places API is enabled
- **Quota errors**: Check API usage and limits
- **CORS errors**: Verify domain restrictions

### Debug Steps
1. Check browser console for JavaScript errors
2. Verify API key in network requests
3. Test API key directly in Google Cloud Console
4. Check API quotas and billing status

---

**Related Documentation:**
- [API Overview](README.md)
- [Frontend Integration](../frontend/google-maps.md)
- [Backend Configuration](../backend/configuration.md)
- [Troubleshooting](../troubleshooting/api-issues.md)
