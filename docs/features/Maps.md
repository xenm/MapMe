# Map & Places Integration

## Overview
Google Maps-based experience to view and interact with DateMarks and clusters.

## User-facing capabilities
- Pan/zoom map with clustered markers
- Click marker to see popup with details and actions (Edit when owner)
- Filter by categories/tags

## Architecture
- Client page: `MapMe.Client/Pages/Map.razor`
- JS interop: `mapInitializer.js` (render map, markers, popups)
- Interop binding: `IJSRuntime` calls from Map.razor

## Key components
- Marker/clustering
- Popup rendering with conditional actions (Edit)
- Current user context for permissions (`window.MapMe.currentUser`)

## Key flows
- Initialize map → load DateMarks → render clusters
- Click marker → popup → actions (navigate to edit context)

## Testing
- Integration tests for map-related API data
- Manual testing for UI interactions and clustering

## Future enhancements
- Draw/select areas
- Heatmaps
- Offline tile support
