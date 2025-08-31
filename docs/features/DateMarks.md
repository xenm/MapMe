# DateMark Management & Editing

## Overview
Comprehensive DateMark management system allowing users to create, view, edit, and delete their location-based memories with rich metadata and duplicate prevention.

## User-Facing Capabilities

### Core DateMark Operations
- **Create DateMark**: Add from map clicks, Google Places, or address search
- **View DateMark**: See details in map popups and profile lists
- **Edit DateMark**: Modify all properties through intuitive interfaces
- **Delete DateMark**: Remove unwanted entries
- **Duplicate Prevention**: Automatic detection and prevention of duplicate entries

### Edit Functionality
- **Map Popup Editing**: Edit button appears for user's own DateMarks in map popups
- **Profile Page Editing**: Comprehensive edit modal accessible from profile DateMark lists
- **Real-time Validation**: Immediate feedback on form inputs and data validation

## Architecture

### Data Models
- **DateMark**: Id, UserId, PlaceId, Name, Note, Categories, Tags, Qualities, Rating, VisitDate, WouldRecommend, Visibility, Location (Latitude/Longitude), Address, CreatedAt, UpdatedAt
- **GeoPoint**: Latitude, Longitude coordinates for spatial operations
- **UserProfile**: Contains DateMarks collection with activity statistics

### Services & Storage
- **UserProfileService**: Manages DateMark CRUD operations and user profile data
- **Storage**: localStorage (development) with repository pattern for production Cosmos DB
- **Serialization**: System.Text.Json throughout the stack

### Client Components
- **Map.razor**: Main map interface with DateMark visualization and popup editing
- **Profile.razor**: User profile with DateMark list and comprehensive edit modal
- **JavaScript Interop**: `mapInitializer.js` handles map rendering and edit button interactions

## Edit Implementation Details

### Map Popup Edit Button
**Location**: Map popup when viewing DateMarks
**Functionality**: 
- Edit button appears only for current user's DateMarks
- Current user identification via `window.MapMe.currentUser`
- JavaScript event handler: `window.MapMe.editDateMark(dateMarkId)`
- Integrates seamlessly with existing map popup system

### Profile Page Edit Modal
**Location**: Profile page DateMark list
**Features**:
- Bootstrap modal dialog with comprehensive form
- All DateMark properties editable except location/address (read-only)
- Real-time form validation and data binding
- Auto-close after successful save with UI feedback

### Editable Properties
- **Place Name**: User-friendly name for the location
- **Note/Description**: Personal memories and details
- **Categories**: Comma-separated location categories
- **Tags**: Comma-separated personal tags
- **Qualities**: Comma-separated quality descriptors
- **Rating**: 1-5 star rating system
- **Visit Date**: HTML5 date input with proper handling
- **Would Recommend**: Boolean checkbox
- **Visibility**: Public/Friends/Private privacy settings

## Duplicate Prevention System

### Detection Methods
- **PlaceId + UserId**: Primary duplicate detection mechanism
- **Spatial Proximity**: Optional threshold-based detection (~25m radius)
- **Normalization**: Case-insensitive comparisons for categories, tags, qualities
- **Place Identifier**: Consistent Google Places ID handling

### User Experience
- **Automatic Detection**: System checks for duplicates before creation
- **Edit Option**: When duplicate detected, offers to edit existing DateMark
- **Merge Capability**: Future enhancement for merging similar entries

## Technical Implementation

### Backend Integration
- **UserProfileService.UpdateDateMarkAsync()**: Handles all edit operations
- **Data Validation**: Server-side validation with comprehensive error handling
- **Statistics Updates**: Real-time activity statistics refresh after edits
- **Persistence**: Atomic operations with rollback capability

### Frontend Features
- **Edit Parameter Support**: URL parameter handling for navigation from map popups
- **Current User Setup**: Proper user identification in Map component
- **Input Parsing**: Comma-separated field handling with trimming and validation
- **Local Data Updates**: Immediate UI refresh without full page reload

### JavaScript Integration
- **Edit Button Rendering**: Dynamic button creation in `showGroupInfo()` function
- **Event Handling**: Proper cleanup and memory management
- **User Context**: Integration with `window.MapMe.currentUser` system

## Testing Coverage

### Unit Tests
- **Business Logic**: DateMark validation, normalization, and duplicate detection
- **Service Layer**: UserProfileService CRUD operations and error handling
- **Data Parsing**: Input validation and comma-separated field processing

### Integration Tests
- **End-to-End CRUD**: Complete DateMark lifecycle testing
- **API Endpoints**: Full coverage of DateMark-related API operations
- **UI Interactions**: Edit modal functionality and form validation

### Manual Testing
- **Cross-browser**: Edit functionality across different browsers
- **Mobile Responsive**: Touch interactions and responsive design
- **User Workflows**: Complete user journeys from creation to editing

## Key User Flows

### Map-Based Editing
1. User clicks DateMark on map → popup appears
2. Edit button visible for user's own DateMarks
3. Click edit → navigates to Profile with edit modal open
4. Complete edit → save → return to map with updated data

### Profile-Based Editing
1. User views Profile page → sees DateMark list
2. Click edit button on any DateMark → modal opens
3. Modify properties → validate → save
4. Modal closes → statistics update → UI refreshes

### Duplicate Prevention Flow
1. User attempts to create DateMark → system checks for duplicates
2. If duplicate found → offers edit existing option
3. User chooses edit → opens existing DateMark in edit modal
4. Complete edit → save → no duplicate created

## Future Enhancements

### Planned Features
- **Batch Operations**: Edit multiple DateMarks simultaneously
- **Merge Duplicates UI**: Visual interface for merging similar entries
- **Conflict Resolution**: Handle concurrent edit conflicts
- **Rich Text Editing**: Enhanced note/description editing capabilities
- **Photo Management**: Integrated photo upload and editing
- **Export/Import**: DateMark data portability

### Technical Improvements
- **Offline Support**: Edit capabilities without internet connection
- **Real-time Sync**: Live updates across multiple devices
- **Advanced Search**: Filter and find DateMarks for editing
- **Audit Trail**: Track edit history and changes

