# User Profiles & Activity Management

## Overview

MapMe provides comprehensive user profile management with editable details, Date Mark collections, activity statistics, and social features for viewing other users' profiles.

## User-Facing Capabilities

### Profile Management
- **View Profiles**: Access your own profile and other users' profiles
- **Edit Profile Details**: Modify personal information, bio, and preferences
- **Photo Management**: Upload, organize, and manage profile photos
- **Privacy Settings**: Control profile visibility and data sharing

### Date Mark Management
- **Date Mark Lists**: View comprehensive lists of your Date Marks
- **Quick Actions**: Edit, delete, and manage Date Marks directly from profile
- **Activity Statistics**: View counts, trends, and engagement metrics
- **"View on Map"**: Navigate directly to Date Mark locations on map

### Social Features
- **User Discovery**: Browse and view other users' profiles
- **Activity Insights**: See public activity statistics and Date Mark collections
- **Profile Interactions**: Start conversations and view shared interests

## Architecture

### Frontend Components
- **Profile Page**: `MapMe.Client/Pages/Profile.razor` - Current user's editable profile
- **User Page**: `MapMe.Client/Pages/User.razor` - Read-only view of other users' profiles
- **Profile Service**: `MapMe.Client/Services/UserProfileService.cs` - Data management and API integration
- **Models**: Client-side data models for profiles, Date Marks, and statistics

### Data Storage
- **Development**: Local storage with JSON serialization
- **Production**: Repository pattern with server-side persistence
- **Caching**: Client-side caching for performance optimization

## Data Models

### UserProfile Model
```csharp
public record UserProfile
{
    public string UserId { get; init; }
    public string Username { get; init; }
    public string DisplayName { get; set; }
    public string Bio { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; }
    public string LookingFor { get; set; }
    public string RelationshipType { get; set; }
    public List<UserPhoto> Photos { get; set; }
    public UserPreferences Preferences { get; set; }
    public ActivityStats Stats { get; set; }
}
```

### ActivityStats Model
```csharp
public record ActivityStats
{
    public int TotalDateMarks { get; init; }
    public int UniqueCategories { get; init; }
    public int UniqueTags { get; init; }
    public int UniqueQualities { get; init; }
    public double AverageRating { get; init; }
    public double RecommendationRate { get; init; }
}
```

### UserPhoto Model
```csharp
public record UserPhoto
{
    public string Id { get; init; }
    public string Url { get; init; }
    public string Caption { get; set; }
    public int Order { get; set; }
    public bool IsPrimary { get; set; }
}
```

## Key User Flows

### Profile Viewing Flow
1. User navigates to `/profile` (own) or `/user/{username}` (others)
2. Component loads user profile data via UserProfileService
3. Activity statistics calculated and displayed
4. Date Mark collections rendered with metadata
5. Photos displayed in responsive gallery layout

### Profile Editing Flow
1. User clicks "Edit Profile" button on Profile page
2. Edit mode activated with form controls
3. User modifies profile information (name, bio, preferences)
4. Changes validated client-side
5. Data saved via UserProfileService
6. Profile refreshed with updated information

### Date Mark Management Flow
1. User views Date Mark list on Profile page
2. Click "Edit" opens Date Mark editing modal
3. User modifies Date Mark details (rating, notes, categories)
4. Changes saved and statistics updated
5. "View on Map" navigates to map with Date Mark highlighted

## Profile Features

### Tinder-Style Dating Fields
- **Basic Information**: Display name, age, gender, bio
- **Dating Preferences**: Looking for, relationship type
- **Personal Details**: Height, location, hometown
- **Professional**: Job title, company, education
- **Lifestyle**: Smoking, drinking, exercise, diet preferences
- **Social**: Languages, interests, hobbies, favorite categories

### Activity Statistics Dashboard
- **Date Mark Metrics**: Total count, unique categories, tags, qualities
- **Engagement Metrics**: Average rating, recommendation rate
- **Visual Indicators**: Progress bars, badges, and color-coded statistics
- **Real-time Updates**: Statistics update automatically when Date Marks change

### Photo Management
- **Gallery Display**: Responsive grid layout for profile photos
- **Photo Upload**: Add new photos via URL input
- **Photo Organization**: Reorder photos and set primary photo
- **Photo Removal**: Delete unwanted photos with confirmation

## Service Integration

### UserProfileService Methods
```csharp
// Profile management
Task<UserProfile> GetCurrentUserProfileAsync()
Task<UserProfile> GetUserProfileAsync(string username)
Task SaveUserProfileAsync(UserProfile profile)

// Date Mark management
Task<List<DateMark>> GetUserDateMarksAsync(string userId)
Task<DateMark> SaveDateMarkAsync(DateMark dateMark)
Task<DateMark> UpdateDateMarkAsync(DateMark dateMark)
Task<bool> DeleteDateMarkAsync(string dateMarkId)

// Statistics
Task<ActivityStats> GetUserActivityStatsAsync(string userId)
```

### Data Persistence
- **Client-side**: JSON serialization to localStorage
- **Server-side**: Repository pattern with database persistence
- **Synchronization**: Automatic sync between client and server
- **Conflict Resolution**: Last-write-wins strategy for concurrent updates

## UI/UX Design

### Responsive Layout
- **Mobile-first**: Optimized for mobile devices
- **Bootstrap Integration**: Consistent styling with Bootstrap 5
- **Card-based Layout**: Information organized in clean cards
- **Progressive Disclosure**: Show/hide details based on context

### Interactive Elements
- **Edit Mode Toggle**: Seamless switching between view and edit modes
- **Modal Dialogs**: Photo management and Date Mark editing
- **Form Validation**: Real-time validation with user feedback
- **Loading States**: Visual feedback during data operations

### Accessibility
- **Keyboard Navigation**: Full keyboard accessibility
- **Screen Reader Support**: Proper ARIA labels and descriptions
- **Color Contrast**: WCAG-compliant color schemes
- **Focus Management**: Logical tab order and focus indicators

## Testing Strategy

### Unit Tests
- **UserProfileService**: Data parsing, validation, and persistence
- **Component Logic**: Profile editing, photo management, statistics calculation
- **Model Validation**: Data integrity and constraint validation

### Integration Tests
- **Profile Endpoints**: API integration testing (when enabled)
- **Data Synchronization**: Client-server data consistency
- **Authentication**: Profile access control and permissions

### Manual Testing
- **Cross-browser**: Testing across different browsers and devices
- **Responsive Design**: Mobile and desktop layout validation
- **User Experience**: End-to-end user journey testing

## Future Enhancements

### Social Features
- **Followers/Following**: Social networking capabilities
- **Friend Requests**: Connection management system
- **Activity Feeds**: Social activity streams
- **Privacy Controls**: Granular privacy settings

### Advanced Statistics
- **Rich Visualizations**: Charts, graphs, and heatmaps
- **Timeline Views**: Activity over time
- **Comparative Analytics**: Compare with other users
- **Export Features**: Data export and reporting

### Media Management
- **Enhanced Gallery**: Advanced photo organization
- **Video Support**: Profile videos and Date Mark videos
- **Media Optimization**: Automatic image compression and resizing
- **Cloud Storage**: Integration with cloud storage providers

---

**Related Documentation:**
- [Frontend Overview](README.md)
- [Maps Integration](maps.md)
- [Date Marks](date-marks.md)
- [User Authentication](../security/authentication.md)
