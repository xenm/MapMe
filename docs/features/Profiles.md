# User Profiles & Dating App Features

## Overview
Comprehensive user profile system with Tinder-style dating app features, activity statistics, photo management, and DateMark integration. Supports both personal profile editing and viewing other users' profiles with rich social features.

## User-Facing Capabilities

### Profile Management
- **Personal Profile Page** (`/profile`): Full editing capabilities for current user
- **User Profile Pages** (`/user/{username}`): Read-only view of other users' profiles
- **Unified Layout**: Identical design between Profile and User pages with conditional editing
- **Real-time Statistics**: Live activity metrics and DateMark analytics

### Dating App Features
- **Comprehensive Profile Fields**: Display name, age, gender, bio, relationship preferences
- **Dating Preferences**: Looking for, relationship type, height, location details
- **Professional Information**: Job title, company, education background
- **Lifestyle Preferences**: Smoking, drinking, exercise, diet, pets, children preferences
- **Social Information**: Languages, interests, hobbies, favorite categories
- **Photo Management**: Multiple photos with captions, primary photo selection, ordering

### Activity & Statistics
- **DateMark Statistics**: Total DateMarks, unique categories, tags, qualities counts
- **Rating Analytics**: Average ratings and recommendation rates
- **Photo Counts**: User photos and place photos statistics
- **Activity Timeline**: Creation dates and recent activity tracking

### DateMark Integration
- **DateMark Lists**: Comprehensive display of user's DateMarks with metadata
- **Edit Functionality**: Direct editing of DateMarks from profile interface
- **Map Integration**: "View on Map" functionality with query parameter navigation
- **Rich Display**: Categories, tags, qualities with color-coded badges

## Architecture

### Client Components
- **Profile.razor**: Personal profile page with full editing capabilities
- **User.razor**: Public user profile page with read-only display
- **UserProfileService**: Client-side service for data management and persistence
- **Bootstrap UI**: Responsive design with cards, modals, and form controls

### Data Models

#### UserProfile (Client-side)
```csharp
public class UserProfile
{
    // Identity
    public string Id { get; set; }
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    
    // Basic Info
    public string? Bio { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    
    // Dating Preferences
    public string? LookingFor { get; set; }
    public string? RelationshipType { get; set; }
    
    // Personal Details
    public string? Height { get; set; }
    public string? Location { get; set; }
    public string? Hometown { get; set; }
    
    // Professional
    public string? JobTitle { get; set; }
    public string? Company { get; set; }
    public string? Education { get; set; }
    
    // Social
    public List<string> Languages { get; set; }
    public List<string> Interests { get; set; }
    public List<string> Hobbies { get; set; }
    public List<string> FavoriteCategories { get; set; }
    
    // Lifestyle
    public LifestylePreferences? Lifestyle { get; set; }
    
    // Photos & Media
    public List<UserPhoto> Photos { get; set; }
    
    // Privacy & Settings
    public string Visibility { get; set; } // "public", "friends", "private"
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### UserProfile (Server-side Record)
```csharp
public sealed record UserProfile(
    string Id,
    string UserId,
    string DisplayName,
    string? Bio,
    IReadOnlyList<UserPhoto> Photos,
    UserPreferences? Preferences,
    string Visibility,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
```

#### Supporting Models
- **UserPhoto**: URL, caption, primary flag, display order
- **LifestylePreferences**: Smoking, drinking, exercise, diet, pets, children preferences
- **ActivityStatistics**: DateMark counts, category/tag/quality statistics, ratings
- **DateMark**: Location-based memories with rich metadata integration

### Data Persistence
- **Client Storage**: localStorage with JSON serialization using System.Text.Json
- **Server Storage**: Cosmos DB with repository pattern for production
- **Dual Model Support**: Client and server models for different use cases
- **Real-time Sync**: Immediate updates between profile and map data

## Technical Implementation

### Profile Loading & Display
```csharp
// Profile.razor - Personal profile with editing
protected override async Task OnInitializedAsync()
{
    _userProfile = await ProfileService.GetCurrentUserProfileAsync();
    _activityStats = await ProfileService.GetUserActivityStatsAsync(_userProfile.UserId);
    _dateMarks = await ProfileService.GetUserDateMarksAsync(_userProfile.UserId);
    _avatarUrl = _userProfile.Photos.FirstOrDefault()?.Url ?? "/images/user-avatar.svg";
}
```

```csharp
// User.razor - Public profile viewing
private async Task LoadUserProfile(string username)
{
    _userProfile = await ProfileService.GetUserProfileAsync(username);
    _activityStats = await ProfileService.GetUserActivityStatsAsync(_userProfile.UserId);
    _dateMarks = await ProfileService.GetUserDateMarksAsync(_userProfile.UserId);
}
```

### Profile Editing System
- **Edit Mode Toggle**: In-place editing with save/cancel functionality
- **Form Validation**: Client-side validation with real-time feedback
- **Data Binding**: Two-way binding for all profile properties
- **Photo Management**: Add/remove photos with URL validation
- **Comma-separated Fields**: Special handling for categories, interests, hobbies

### Activity Statistics Integration
- **Real-time Calculation**: Live statistics based on user's DateMarks
- **Comprehensive Metrics**: Total counts, unique values, averages, rates
- **Visual Display**: Statistics cards with color-coded metrics
- **Performance Optimized**: Efficient calculation and caching

## User Workflows

### Personal Profile Management
1. **Access Profile**: Navigate to `/profile` page
2. **View Data**: See comprehensive profile information and statistics
3. **Edit Mode**: Toggle edit mode to modify profile details
4. **Update Fields**: Modify any profile field with real-time validation
5. **Photo Management**: Add/remove photos, set primary photo
6. **Save Changes**: Persist updates with immediate UI refresh

### Viewing Other Users
1. **User Discovery**: Navigate to `/user/{username}` from various sources
2. **Profile Display**: View comprehensive user information (read-only)
3. **Activity Stats**: See user's DateMark statistics and activity
4. **Photo Gallery**: Browse user's photos with lightbox functionality
5. **DateMark Exploration**: View user's DateMarks with "View on Map" links

### DateMark Management from Profile
1. **DateMark List**: View all personal DateMarks with rich metadata
2. **Edit DateMark**: Click edit button to open comprehensive edit modal
3. **Update Properties**: Modify categories, tags, qualities, ratings, notes
4. **Map Integration**: "View on Map" navigation with query parameters
5. **Delete DateMark**: Remove DateMarks with confirmation

### Photo Management
1. **Photo Gallery**: View all user photos in responsive grid layout
2. **Add Photos**: Add new photos via URL input with validation
3. **Set Primary**: Designate primary photo for avatar display
4. **Photo Ordering**: Manage display order of photos
5. **Remove Photos**: Delete photos with confirmation

## Data Integration & Services

### UserProfileService Methods
- **GetCurrentUserProfileAsync()**: Load current user's profile
- **GetUserProfileAsync(username)**: Load any user's profile by username
- **SaveCurrentUserProfileAsync(profile)**: Persist current user's profile changes
- **GetUserActivityStatsAsync(userId)**: Calculate and return activity statistics
- **GetUserDateMarksAsync(userId)**: Load all DateMarks for a user
- **SaveDateMarkAsync(dateMark)**: Save new DateMark with duplicate prevention
- **UpdateDateMarkAsync(dateMark)**: Update existing DateMark
- **DeleteDateMarkAsync(dateMarkId)**: Remove DateMark

### Storage & Persistence
- **localStorage Keys**: Separate keys for profiles, DateMarks, and all users
- **JSON Serialization**: System.Text.Json for all data serialization
- **Data Validation**: Input validation and sanitization throughout
- **Error Handling**: Comprehensive error handling with user feedback

### Repository Pattern (Server-side)
- **IUserProfileRepository**: Interface for user profile data access
- **CosmosUserProfileRepository**: Production Cosmos DB implementation
- **InMemoryUserProfileRepository**: Testing and development implementation
- **CRUD Operations**: Full create, read, update, delete support

## Privacy & Security

### Privacy Controls
- **Visibility Settings**: Public, friends, private profile visibility
- **Photo Privacy**: Secure photo URL handling and access controls
- **DateMark Privacy**: Individual DateMark visibility settings
- **Profile Data Protection**: Secure handling of personal information

### Data Security
- **Input Validation**: Comprehensive validation of all user inputs
- **XSS Prevention**: Content sanitization for user-generated content
- **Secure Storage**: Encrypted storage of sensitive profile data
- **Access Controls**: Owner-only editing with UI and API validation

## Testing Coverage

### Unit Tests
- **UserProfileService**: Data loading, saving, validation, and error handling
- **Profile Components**: UI interactions, form validation, and state management
- **Data Models**: Serialization, validation, and business logic
- **Repository Pattern**: CRUD operations and data persistence

### Integration Tests
- **Profile API**: End-to-end profile management workflows
- **Authentication**: Profile access control and permission validation
- **DateMark Integration**: Profile-DateMark data consistency
- **Photo Management**: Photo upload, validation, and storage

### Manual Testing
- **Cross-browser**: Profile functionality across different browsers
- **Mobile Responsive**: Touch interactions and responsive design
- **Performance**: Large dataset handling and UI responsiveness
- **User Experience**: Complete user journeys and edge cases

## Future Enhancements

### Social Features
- **Followers/Following**: Social connection system
- **Friend Requests**: Connection management and approval
- **Activity Feed**: Social activity timeline and updates
- **Profile Sharing**: Share profiles via links and social media

### Advanced Analytics
- **Rich Statistics**: Heatmaps, timelines, and trend analysis
- **Comparative Analytics**: Compare activity with other users
- **Goal Tracking**: Personal goals and achievement tracking
- **Export Features**: Data export and backup capabilities

### Enhanced Media
- **Video Support**: Profile videos and DateMark videos
- **Photo Editing**: In-app photo editing and filters
- **Media Gallery**: Advanced photo and video management
- **Storage Integration**: Cloud storage integration for media

### Personalization
- **Custom Themes**: Personalized profile themes and layouts
- **Advanced Preferences**: Granular privacy and notification settings
- **Profile Templates**: Pre-designed profile layouts and styles
- **Recommendation Engine**: Personalized user and place recommendations

