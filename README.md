# MapMe

![.NET](https://img.shields.io/badge/.NET-10-blue) ![Blazor](https://img.shields.io/badge/Blazor-WASM%20%2B%20Interactive%20SSR-purple) ![Tests](https://img.shields.io/badge/Tests-Unit%20%2F%20Integration-green) [![License](https://img.shields.io/badge/License-Open%20Source-lightgrey)](./LICENSE)

A modern Blazor dating app with Google Maps integration, featuring comprehensive user profiles, location-based date marking, and social discovery features.

## Features

### 🗺️ Interactive Map Experience
- **Google Maps Integration**: Full-featured map with search, geolocation, and place details
- **Date Mark Creation**: Click anywhere on the map to create and save memorable date locations
- **Place Discovery**: Search for locations, get place details, and view photos
- **Real-time Navigation**: Navigate between map locations with query parameters
- **Duplicate Prevention**: Prevents creating multiple Date Marks for the same location

### 👤 Comprehensive User Profiles
- **Tinder-Style Fields**: Complete dating app profile system with:
  - Basic Info: Display name, age, gender, bio (500 character limit)
  - Dating Preferences: Looking for (men/women/everyone), relationship type
  - Personal Details: Height, location, hometown
  - Professional Info: Job title, company, education
  - Lifestyle Preferences: Smoking, drinking, exercise, diet, pets, children
  - Social Info: Languages, interests, hobbies, favorite categories
- **Photo Management**: Upload, organize, and manage profile photos with captions
- **Privacy Controls**: Public, friends-only, or private profile visibility settings

### 📊 Activity Statistics
- **Real-time Metrics**: Track Date Marks, categories, tags, and qualities
- **Rating System**: 1-5 star ratings and recommendation tracking
- **Social Analytics**: View activity statistics on both your profile and other users' profiles

### 🧭 Navigation & Discovery
- **Two Main Screens**: Simplified navigation between Map and Profile
- **User Discovery**: Browse other users' profiles at `/user/{username}`
- **Profile Editing**: Full editing capabilities on your own profile page
- **Unified Layout**: Consistent design between profile viewing and editing modes

## Quick Start

### Prerequisites
- .NET 10 SDK (preview)
- Google Maps API key
- Modern web browser with JavaScript enabled

### Running the Application
**Using IDE (Rider/Visual Studio):**
- Open the solution and run the "MapMe" project

**Using CLI:**
```bash
dotnet run --project MapMe/MapMe/MapMe.csproj
```

**Development URL:**
- HTTPS: https://localhost:8008

## Configuration

### Google Maps API Key Setup
The application requires a Google Maps API key for map functionality. Keys are never committed to source control.

**Development (Recommended - User Secrets):**
```bash
cd MapMe/MapMe/MapMe
dotnet user-secrets init
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-api-key"
```

**Production/CI (Environment Variable):**
```bash
export GOOGLE_MAPS_API_KEY="your-google-api-key"
```

**Key Lookup Order:**
1. `GoogleMaps:ApiKey` from configuration (includes User Secrets)
2. `GOOGLE_MAPS_API_KEY` environment variable

### Google Maps API Requirements
Enable these APIs in Google Cloud Console:
- Maps JavaScript API
- Places API
- Geocoding API

**Security Configuration:**
- Restrict API key to your domain/localhost in Google Cloud Console
- Set API restrictions to only the required Google Maps APIs

## Architecture

### Technology Stack
- **Frontend**: Blazor WebAssembly + Interactive SSR
- **Backend**: ASP.NET Core (.NET 10)
- **Data**: In-memory repositories with localStorage persistence, Azure Cosmos DB support
- **Maps**: Google Maps JavaScript API with Blazor JS Interop
- **Serialization**: System.Text.Json exclusively (including custom Cosmos DB serializer)

### Project Structure
```
MapMe/
├── MapMe/                          # Server project (ASP.NET Core)
│   ├── Controllers/                # API controllers for DateMarks
│   ├── Models/                     # Server-side data models
│   ├── Repositories/               # Data access layer
│   └── Program.cs                  # Server configuration
├── MapMe.Client/                   # Client project (Blazor WebAssembly)
│   ├── Pages/                      # Blazor pages (Map, Profile, User)
│   ├── Services/                   # Client services (UserProfileService)
│   ├── Models/                     # Client-side models
│   └── wwwroot/js/                 # JavaScript interop files
└── MapMe.Tests/                    # Unit and integration tests
```

### Key Services

#### UserProfileService
Central service for profile and DateMark management:
- **Profile Management**: Create, read, update user profiles
- **DateMark CRUD**: Full lifecycle management of date locations
- **Activity Statistics**: Real-time calculation of user metrics
- **Duplicate Prevention**: Checks for existing DateMarks by location and user
- **Data Persistence**: localStorage integration with JSON serialization

#### Repository Pattern
- **IUserProfileRepository**: User profile data access
- **IDateMarkByUserRepository**: DateMark data access with filtering
- **In-Memory Implementation**: Fast development and testing
- **Cosmos DB Implementation**: Production-ready with geospatial queries

#### Custom Cosmos DB Serialization
- **SystemTextJsonCosmosSerializer**: Custom serializer eliminating Newtonsoft.Json dependency
- **Consistent JSON Handling**: Single serialization library across entire application
- **Performance Optimized**: Uses System.Text.Json for better performance and memory usage
- **Security Enhanced**: Eliminates vulnerable dependencies while maintaining full functionality

## Pages & Navigation

### Map Page (`/`)
- **Interactive Google Maps**: Click to create Date Marks
- **Location Search**: Find and navigate to specific places
- **Current Location**: Geolocation support with fallback
- **Place Details**: Rich information from Google Places API
- **Date Mark Management**: Create, edit, and view saved locations

### Profile Page (`/profile`)
- **Personal Dashboard**: View and edit your complete profile
- **Photo Management**: Upload, organize, and manage profile photos
- **DateMark History**: View all your saved locations with map navigation
- **Activity Statistics**: Real-time metrics and achievements
- **Privacy Settings**: Control profile visibility

### User Page (`/user/{username}`)
- **Public Profiles**: View other users' profiles (read-only)
- **Social Discovery**: Browse photos, interests, and Date Marks
- **Activity Insights**: View other users' statistics and preferences
- **Map Integration**: Navigate to users' Date Mark locations

## Data Models

### UserProfile
Complete dating app profile with:
- Personal information (name, age, gender, bio)
- Dating preferences and relationship goals
- Professional and lifestyle details
- Photo collection with metadata
- Privacy and visibility settings

### DateMark
Location-based memories with:
- Geographic coordinates and place details
- Categories, tags, and qualities for organization
- Ratings and recommendations
- Visit dates and creation timestamps
- Privacy controls and sharing settings

### ActivityStatistics
Real-time user metrics:
- Total Date Marks and unique locations
- Category and tag diversity
- Average ratings and recommendation rates
- Social engagement indicators

## JavaScript Integration

### Map Initialization (`mapInitializer.js`)
- **Secure API Loading**: Fetches Google Maps key from server
- **Interactive Features**: Click handlers, marker management, search
- **Real User Data**: Integrates with Blazor for authentic profile information
- **Photo Integration**: Displays real user photos in map popups
- **Place Discovery**: Rich place details with photos and reviews

### Blazor JS Interop
- **Bidirectional Communication**: C# ↔ JavaScript integration
- **Real-time Updates**: Map state synchronization with Blazor components
- **User Profile Hooks**: JavaScript access to real user profile data
- **Photo Viewer**: Lightbox integration for photo galleries

## Security & Privacy

### Data Protection
- **No API Keys in Source**: All secrets managed via configuration
- **Client-side Storage**: User data stored locally with JSON serialization
- **Privacy Controls**: Granular visibility settings for profiles and Date Marks

### Best Practices
- **HTTPS Only**: Secure communication in all environments
- **API Key Restrictions**: Google Maps keys restricted by domain and API
- **Input Validation**: Comprehensive validation on both client and server
- **Error Handling**: Graceful degradation and user-friendly error messages

### Secure Logging Policy
- No raw JWT tokens or Authorization headers are ever logged. Only sanitized previews via `ToTokenPreview()`.
- Emails are treated as sensitive. Logs include only metadata (e.g., `HasEmail`, `EmailLength`) — never the raw address.
- All user-controlled values are sanitized with `SanitizeForLog()` to remove newlines and prevent log injection.

## Development

### Documentation
- Full docs index: docs/README.md
- Setup guide: docs/setup.md
- Architecture: docs/architecture.md
- Testing: docs/testing.md

### Running Tests
```bash
# Unit tests (fast)
dotnet test MapMe/MapMe/MapMe.Tests --filter "Category=Unit"

# Integration tests (in-memory repositories)
dotnet test MapMe/MapMe/MapMe.Tests --filter "Category!=Unit"

# All tests
dotnet test MapMe/MapMe/MapMe.Tests
```

### Building for Production
```bash
dotnet publish -c Release
```

### Code Quality
- **Nullable Reference Types**: Enabled for better null safety
- **.NET 10 Features**: Latest C# language features and performance improvements
- **System.Text.Json**: Modern JSON serialization following .NET best practices
- **Responsive Design**: Bootstrap-based UI with mobile-first approach

## Troubleshooting

### Common Issues

**Port Already in Use:**
- Kill existing processes on ports 5260/7160 or update `launchSettings.json`

**Google Maps Not Loading:**
- Verify API key is correctly configured
- Check Google Cloud Console for API restrictions
- Ensure required APIs are enabled

**Profile Data Not Persisting:**
- Check browser localStorage permissions
- Verify UserProfileService is registered in DI container
- Check browser console for serialization errors

**Map Click Not Working:**
- Ensure JavaScript files are loaded correctly
- Check browser console for JS errors
- Verify Blazor JS interop is functioning

### Debug Mode
Enable detailed logging by setting environment variable:
```bash
export ASPNETCORE_ENVIRONMENT=Development
```

## Contributing

### Development Setup
1. Clone the repository
2. Configure Google Maps API key (see Configuration section)
3. Run `dotnet restore` to restore packages
4. Run the application using your preferred method

### Code Style
- Follow .NET coding conventions
- Use System.Text.Json for serialization
- Implement proper error handling and logging
- Write unit tests for new features

## License

This project is for educational and demonstration purposes. Please ensure you comply with Google Maps API terms of service when using this application.

---

**MapMe** - Where every location tells a story. 🗺️💕
