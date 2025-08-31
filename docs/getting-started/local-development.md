# Local Development Setup

This comprehensive guide will help you set up a complete local development environment for MapMe.

## Prerequisites

Before starting, ensure you have completed the [Prerequisites Guide](./prerequisites.md):
- ✅ .NET 10 SDK (preview)
- ✅ Git
- ✅ Modern web browser
- ✅ Google Maps API key configured

## Complete Setup Process

### 1. Repository Setup

```bash
# Clone the repository
git clone https://github.com/xenm/MapMe.git
cd MapMe

# Verify repository structure
ls -la
# Should show: MapMe/, docs/, Scripts/, README.md, etc.
```

### 2. .NET Configuration

```bash
# Verify .NET version
dotnet --version
# Should show: 10.0.100-preview.x.xxxxx.x

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

### 3. Google Maps API Configuration

#### Using User Secrets (Recommended)
```bash
# Navigate to server project
cd MapMe/MapMe/MapMe

# Initialize user secrets
dotnet user-secrets init

# Set Google Maps API key
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-maps-api-key"

# Verify configuration
dotnet user-secrets list
# Should show: GoogleMaps:ApiKey = your-key-here

# Return to root directory
cd ../../..
```

#### Alternative: Environment Variable
```bash
# Set environment variable (session-specific)
export GOOGLE_MAPS_API_KEY="your-google-maps-api-key"

# For persistent setting, add to your shell profile
echo 'export GOOGLE_MAPS_API_KEY="your-google-maps-api-key"' >> ~/.bashrc
source ~/.bashrc
```

### 4. Database Configuration

MapMe uses in-memory repositories for local development by default. No additional setup required.

#### Optional: Cosmos DB Emulator Setup
```bash
# Start Cosmos DB Emulator (if desired)
./Scripts/start-cosmos.ps1

# Initialize database
./Scripts/init-cosmosdb.ps1
```

### 5. Running the Application

#### Using .NET CLI
```bash
# Run from root directory
dotnet run --project MapMe/MapMe/MapMe.csproj

# Application will start at:
# HTTPS: https://localhost:8008
# HTTP: http://localhost:8007 (redirects to HTTPS)
```

#### Using IDE
- **JetBrains Rider**: Open `MapMe.sln` → Run "MapMe" configuration
- **Visual Studio**: Open `MapMe.sln` → Set MapMe as startup project → F5
- **VS Code**: Open folder → Run "Launch MapMe" configuration

### 6. Verification Steps

#### 1. Application Health Check
Navigate to: `https://localhost:8008/health`
Should return: `Healthy`

#### 2. Google Maps Integration
1. Open: `https://localhost:8008`
2. Create account or sign in
3. Verify map loads correctly
4. Test location search functionality

#### 3. API Endpoints
Test key endpoints:
```bash
# Health check
curl https://localhost:8008/health

# Maps configuration (requires authentication)
curl https://localhost:8008/config/maps
```

## Development Workflow

### 1. Daily Development
```bash
# Start development session
cd MapMe
dotnet run --project MapMe/MapMe/MapMe.csproj

# In another terminal - run tests
dotnet test MapMe/MapMe.Tests

# Watch for changes (optional)
dotnet watch run --project MapMe/MapMe/MapMe.csproj
```

### 2. Making Changes
1. **Create Feature Branch**: `git checkout -b feature/your-feature`
2. **Make Changes**: Edit code with hot reload support
3. **Run Tests**: `dotnet test` to verify changes
4. **Test Manually**: Verify functionality in browser
5. **Commit Changes**: Follow commit message conventions

### 3. Testing Your Changes
```bash
# Run all tests
dotnet test MapMe/MapMe.Tests

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## IDE-Specific Setup

### JetBrains Rider (Recommended)
1. **Open Solution**: File → Open → Select `MapMe.sln`
2. **Configure Run**: Use existing "MapMe" run configuration
3. **Enable Hot Reload**: Settings → Build → Enable hot reload
4. **Set Breakpoints**: Full debugging support available

### Visual Studio 2022
1. **Open Solution**: File → Open → Project/Solution → Select `MapMe.sln`
2. **Set Startup Project**: Right-click MapMe project → Set as Startup Project
3. **Configure Debugging**: F5 for debug mode, Ctrl+F5 for release
4. **Package Manager**: Tools → NuGet Package Manager for dependencies

### Visual Studio Code
1. **Open Folder**: File → Open Folder → Select MapMe root directory
2. **Install Extensions**: C# Dev Kit, GitLens recommended
3. **Configure Launch**: Use `.vscode/launch.json` configuration
4. **Integrated Terminal**: Use for dotnet commands

## Common Development Tasks

### Adding New Features
1. **Create Models**: Add to appropriate Models folder
2. **Create Services**: Implement business logic in Services
3. **Create Controllers**: Add API endpoints in Controllers
4. **Create Components**: Add Blazor components in Client/Pages or Components
5. **Write Tests**: Add unit and integration tests
6. **Update Documentation**: Document new features

### Database Development
```bash
# Using in-memory repositories (default)
# No additional setup required

# Using Cosmos DB Emulator (optional)
./Scripts/start-cosmos.ps1
# Update appsettings.Development.json with Cosmos connection
```

### Frontend Development
```bash
# Client-side development
cd MapMe/MapMe.Client

# JavaScript files location
# wwwroot/js/mapInitializer.js - Google Maps integration
# wwwroot/css/ - Custom stylesheets
```

## Troubleshooting

### Common Issues

#### Port Already in Use
```bash
# Find processes using ports
lsof -i :8008
lsof -i :8007

# Kill processes
kill -9 <PID>

# Or update launchSettings.json with different ports
```

#### Google Maps Not Loading
1. **Verify API Key**: Check user secrets configuration
2. **Check Console**: Browser developer tools for errors
3. **API Restrictions**: Verify Google Cloud Console settings
4. **Enabled APIs**: Ensure Maps JavaScript API, Places API, Geocoding API are enabled

#### Build Errors
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build

# Check .NET version
dotnet --version

# Update global tools if needed
dotnet tool update -g dotnet-ef
```

#### Authentication Issues
1. **JWT Configuration**: Check JWT settings in appsettings
2. **User Secrets**: Verify configuration with `dotnet user-secrets list`
3. **Browser Storage**: Clear localStorage and cookies
4. **Token Expiration**: Check token validity and refresh

### Getting Help
- **Documentation**: Check [Troubleshooting Guide](../troubleshooting/README.md)
- **GitHub Issues**: Search existing issues or create new one
- **Team Chat**: Ask in development team channels
- **Code Review**: Request help during code review process

## Next Steps

After successful setup:
1. **Explore Codebase**: Familiarize yourself with project structure
2. **Run Tests**: Understand test coverage and patterns
3. **Make First Change**: Try a small feature or bug fix
4. **Read Architecture**: Review [Architecture Documentation](../architecture/README.md)
5. **Contribute**: Follow [Contributing Guidelines](../development/contributing.md)

## Performance Tips

### Development Performance
- **Hot Reload**: Use `dotnet watch` for automatic recompilation
- **Parallel Testing**: Run tests in parallel for faster feedback
- **IDE Optimization**: Configure IDE for optimal performance
- **Resource Monitoring**: Monitor CPU and memory usage during development

### Debugging Tips
- **Structured Logging**: Use Serilog for comprehensive logging
- **Breakpoint Debugging**: Set breakpoints in both C# and JavaScript
- **Network Inspection**: Use browser dev tools for API calls
- **Database Queries**: Monitor Cosmos DB query performance

---

**Estimated Setup Time**: 30-45 minutes  
**Last Updated**: 2025-08-30  
**Maintained By**: Development Team
