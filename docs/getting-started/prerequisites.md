# Prerequisites

This guide covers all the tools and dependencies required to develop and run MapMe.

## Required Tools

### .NET 10 SDK (Preview)
MapMe uses .NET 10 preview features.

**Installation:**
```bash
# Download from Microsoft
# https://dotnet.microsoft.com/download/dotnet/10.0

# Verify installation
dotnet --version
# Should show: 10.0.100-preview.x.xxxxx.x
```

### Git
Version control system for source code management.

**Installation:**
- **Windows**: Download from [git-scm.com](https://git-scm.com/)
- **macOS**: `brew install git` or Xcode Command Line Tools
- **Linux**: `sudo apt install git` (Ubuntu/Debian)

### Modern Web Browser
Required for testing the Blazor WebAssembly application.

**Recommended:**
- Chrome 90+ (best developer tools)
- Firefox 88+
- Safari 14+ (macOS)
- Edge 90+

## Google Maps API Setup

MapMe requires Google Maps API access for location features.

### 1. Create Google Cloud Project
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable billing (required for Maps APIs)

### 2. Enable Required APIs
Enable these APIs in your Google Cloud project:
- **Maps JavaScript API** - Core map functionality
- **Places API** - Location search and details
- **Geocoding API** - Address to coordinates conversion

### 3. Create API Key
1. Go to **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **API Key**
3. Copy the generated API key

### 4. Secure API Key
**Application Restrictions:**
- HTTP referrers: `localhost:*`, `127.0.0.1:*`, `your-domain.com/*`

**API Restrictions:**
- Restrict to: Maps JavaScript API, Places API, Geocoding API

### 5. Configure in MapMe
```bash
cd MapMe/MapMe/MapMe
dotnet user-secrets init
dotnet user-secrets set "GoogleMaps:ApiKey" "your-api-key-here"
```

## Optional Tools

### Development Environment
**Recommended IDEs:**
- **JetBrains Rider** (preferred) - Excellent .NET support
- **Visual Studio 2022** - Full-featured Microsoft IDE
- **Visual Studio Code** - Lightweight with C# extension

### Database Tools
**For Cosmos DB development:**
- **Azure Cosmos DB Emulator** - Local development database
- **Azure Storage Explorer** - Database management GUI

**Installation:**
```bash
# Cosmos DB Emulator (Windows)
# Download from Microsoft Azure documentation

# Docker alternative (cross-platform)
docker run -p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
```

### Testing Tools
**For enhanced testing:**
- **Postman** - API testing
- **Browser DevTools** - Frontend debugging
- **dotnet-reportgenerator-globaltool** - Test coverage reports

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

## Environment Verification

Run this checklist to verify your setup:

```bash
# Check .NET version
dotnet --version

# Check Git
git --version

# Clone and test build
git clone https://github.com/xenm/MapMe.git
cd MapMe
dotnet restore
dotnet build

# Verify Google Maps API key
cd MapMe/MapMe/MapMe
dotnet user-secrets list
# Should show: GoogleMaps:ApiKey = your-key-here
```

## Next Steps

Once prerequisites are installed:
1. **Quick Setup**: [Quick Start Guide](./quick-start.md)
2. **Full Development**: [Local Development Setup](./local-development.md)
3. **First Contribution**: [First Contribution Guide](./first-contribution.md)

## Troubleshooting

| Issue | Solution |
|-------|----------|
| .NET 10 not found | Download from Microsoft .NET downloads page |
| Google Maps quota exceeded | Check billing and usage limits in Google Cloud Console |
| API key not working | Verify restrictions and enabled APIs |
| Build errors | Run `dotnet clean` then `dotnet restore` |

---

**Last Updated**: 2025-08-30  
**Review Schedule**: Monthly
