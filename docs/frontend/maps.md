# Maps & Places Integration

## Overview

MapMe provides an interactive Google Maps-based experience for viewing and interacting with Date Marks, featuring clustering, filtering, and real-time user interactions.

## User-Facing Capabilities

### Core Map Features
- **Interactive Navigation**: Pan and zoom map with smooth performance
- **Clustered Markers**: Automatic clustering of nearby Date Marks for better performance
- **Marker Interactions**: Click markers to see detailed popups with place information
- **Filtering**: Filter Date Marks by categories, tags, and other criteria
- **User Context**: Different actions available based on ownership (Edit for own Date Marks)

### Date Mark Management
- **Create Date Marks**: Click on Google Places or double-click map locations
- **Edit Date Marks**: Modify existing Date Marks (owner only)
- **View Details**: See comprehensive Date Mark information in popups
- **Navigation**: "View on Map" functionality from Profile pages

## Architecture

### Frontend Components
- **Main Component**: `MapMe.Client/Pages/Map.razor`
- **JavaScript Interop**: `mapInitializer.js` handles map rendering, markers, and popups
- **Blazor Integration**: `IJSRuntime` calls from Map.razor to JavaScript functions
- **State Management**: Component-level state for current user, Date Marks, and filters

### Key Technical Files
```
MapMe.Client/Pages/Map.razor          - Main Blazor component
MapMe.Client/wwwroot/js/mapInitializer.js - Google Maps JavaScript integration
MapMe.Client/Services/UserProfileService.cs - Data management
MapMe.Client/Models/UserProfile.cs    - Client-side data models (includes DateMark class)
```

## Key Components

### Marker System
- **Clustering Algorithm**: Groups nearby markers for performance
- **Custom Markers**: Styled markers with user photos and place information
- **Hover Effects**: Preview information on marker hover
- **Click Handlers**: Open detailed popups with actions

### Popup Rendering
- **Dynamic Content**: Place photos, user information, Date Mark details
- **Conditional Actions**: Edit button appears only for Date Mark owners
- **Google Maps Links**: Clickable place names linking to Google Maps
- **User Profiles**: Hover cards with user information and photos

### Current User Context
- **Authentication Integration**: `window.MapMe.currentUser` for permissions
- **Ownership Detection**: Show/hide edit functionality based on user ownership
- **Profile Integration**: Access to current user profile data

## Key User Flows

### Map Initialization Flow
1. Component loads and initializes Google Maps API
2. Fetch current user profile and Date Marks
3. Render clustered markers on map
4. Set up event handlers for interactions

### Date Mark Creation Flow
1. User clicks on Google Place or double-clicks map
2. Place details fetched from Google Places API
3. Creation popup displayed with place information
4. User fills in Date Mark details (rating, notes, categories)
5. Date Mark saved and marker added to map

### Date Mark Editing Flow
1. User clicks on their own Date Mark marker
2. Popup displays with Edit button (owner only)
3. Edit button navigates to Profile page with edit context
4. User modifies Date Mark details
5. "View on Map" returns to map with updated marker

## Google Maps Integration

### API Configuration
- **Maps JavaScript API**: Core mapping functionality
- **Places API**: Place search and details
- **Geocoding API**: Address to coordinate conversion

### JavaScript Functions
```javascript
// Core map functions
initializeMap()           - Initialize Google Maps
renderDateMarks()         - Display Date Mark markers
showDateMarkPopup()       - Show Date Mark details popup
createDateMark()          - Create new Date Mark
updateDateMark()          - Update existing Date Mark

// User interaction functions
handleMapClick()          - Handle map click events
handleMarkerClick()       - Handle marker click events
showUserProfile()         - Display user profile popup
```

### Blazor Interop
```csharp
// JavaScript interop methods
await JSRuntime.InvokeVoidAsync("initializeMap", mapOptions);
await JSRuntime.InvokeVoidAsync("renderDateMarks", dateMarks);
var result = await JSRuntime.InvokeAsync<bool>("createDateMark", dateMark);
```

## Performance Optimization

### Marker Clustering
- Automatic clustering of nearby markers (within ~25m)
- Dynamic cluster sizing based on zoom level
- Efficient rendering for large numbers of Date Marks

### Lazy Loading
- Place photos loaded on demand
- User profile data cached locally
- Progressive loading of Date Mark details

### Caching Strategy
- Google Places API responses cached
- User profile data stored in component state
- Map tiles cached by Google Maps API

## Testing Strategy

### Integration Tests
- API endpoint testing for Date Mark CRUD operations
- User profile service testing
- Authentication and authorization testing

### Manual Testing
- UI interactions and clustering behavior
- Cross-browser compatibility testing
- Mobile responsiveness testing
- Performance testing with large datasets

### Test Scenarios
```
✓ Map loads with API key and centers correctly
✓ Date Marks render with proper clustering
✓ Marker popups display correct information
✓ Edit functionality works for Date Mark owners
✓ Google Maps links work in popups
✓ User profile popups display correctly
✓ Filter functionality works as expected
```

## Future Enhancements

### Advanced Map Features
- **Area Selection**: Draw and select geographic areas
- **Heatmaps**: Visualize Date Mark density
- **Custom Map Styles**: Themed map appearances
- **Offline Support**: Cached tiles for offline viewing

### Enhanced Interactions
- **Real-time Updates**: Live Date Mark updates from other users
- **Social Features**: See friends' Date Marks with privacy controls
- **Advanced Filtering**: Date ranges, ratings, user-specific filters
- **Export Features**: Export Date Marks to various formats

### Performance Improvements
- **Virtual Scrolling**: For large numbers of markers
- **WebGL Rendering**: Hardware-accelerated marker rendering
- **Service Workers**: Offline functionality and caching
- **Progressive Web App**: Enhanced mobile experience

---

**Related Documentation:**
- [Frontend Overview](README.md)
- [Google Maps Integration](../api/google-maps-integration.md)
- [User Profiles](profiles.md)
- [Date Marks](date-marks.md)
