# Complete Local Development Guide

This comprehensive guide covers everything you need to develop MapMe locally - from initial setup to advanced
development workflows.

## Prerequisites & Installation

### Required Tools

#### .NET 10 SDK (Preview)

MapMe uses .NET 10 preview features.

```bash
# Download from Microsoft
# https://dotnet.microsoft.com/download/dotnet/10.0

# Verify installation
dotnet --version
# Should show: 10.0.100-preview.x.xxxxx.x
```

#### Git Configuration

```bash
# Set up Git identity
git config --global user.name "Your Name"
git config --global user.email "your.email@domain.com"

# Enable signed commits (recommended)
git config --global commit.gpgsign true
```

#### Modern Web Browser

Required for testing the Blazor WebAssembly application.

- Chrome 90+ (best developer tools)
- Firefox 88+ / Safari 14+ / Edge 90+

### Google Maps API Setup

MapMe requires Google Maps API access for location features.

#### 1. Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable billing (required for Maps APIs)

#### 2. Enable Required APIs

- **Maps JavaScript API** - Core map functionality
- **Places API** - Location search and details
- **Geocoding API** - Address to coordinates conversion

#### 3. Create and Secure API Key

1. Go to **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **API Key**
3. Configure restrictions:
    - **HTTP referrers**: `localhost:*`, `127.0.0.1:*`
    - **API restrictions**: Maps JavaScript API, Places API, Geocoding API

## Development Environment Setup

### Recommended IDE Configuration

#### Primary: JetBrains Rider + Cascade + Claude Sonnet 4+

- **JetBrains Rider**: Superior .NET 10 preview support, integrated debugging
- **Cascade Extension**: AI-powered coding assistant with Claude Sonnet 4+
- **SonarQube for IDE**: Real-time code quality analysis
- **Qodana Extension**: Continuous inspection and quality gates

#### Rider Commit Settings (Recommended)

Configure pre-commit actions in Rider's commit dialog:

- ✅ **Sign-off commit** - Adds `Signed-off-by` for contribution tracking
- ✅ **Reformat code** - Applies consistent formatting
- ✅ **Rearrange code** - Organizes using statements and class members
- ✅ **Optimize imports** - Removes unused imports
- ✅ **Perform SonarQube for IDE analysis** - Quality checks before commit
- ✅ **Check TODO** - Validates TODO comments
- ✅ **Run 'test-all.sh'** - Full test suite execution
- ✅ **Run advanced checks after commit** - Post-commit validation

### GPG Key Setup (Recommended for Signed Commits)

**Why GPG Signing?**

- Verifies commit authenticity
- Required for enterprise environments
- Shows verified badges on GitHub

**Setup Steps:**

```bash
# Generate GPG key (RSA 4096-bit, 2-year expiration)
gpg --full-generate-key

# List and export public key
gpg --list-secret-keys --keyid-format=long
gpg --armor --export KEY_ID

# Configure Git to use GPG
git config --global user.signingkey KEY_ID
git config --global commit.gpgsign true
git config --global tag.gpgsign true

# macOS GPG agent configuration
export GPG_TTY=$(tty)
echo "pinentry-program /usr/local/bin/pinentry-mac" >> ~/.gnupg/gpg-agent.conf
gpgconf --kill gpg-agent
```

**Add GPG key to GitHub**: Settings → SSH and GPG keys → New GPG key

## Repository Setup & Configuration

### 1. Clone and Initial Setup
```bash
# Clone the repository
git clone https://github.com/xenm/MapMe.git
cd MapMe

# Verify repository structure
ls -la
# Should show: src/, docs/, scripts/, README.md, etc.

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

### 2. Configure Google Maps API

#### Using User Secrets (Recommended)
```bash
# Navigate to server project
cd src/MapMe

# Initialize user secrets
dotnet user-secrets init

# Set Google Maps API key
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-maps-api-key"

# Verify configuration
dotnet user-secrets list
# Should show: GoogleMaps:ApiKey = your-key-here

# Return to root directory
cd ../..
```

### 3. Database Configuration

MapMe uses in-memory repositories for local development by default. No additional setup required.

#### Optional: Cosmos DB Emulator Setup
```bash
# Start Cosmos DB Emulator (cross-platform)
./scripts/start-cosmos.sh

# Initialize database
./scripts/init-cosmosdb.sh
```

### 4. Running the Application

#### Using .NET CLI
```bash
# Run from root directory
dotnet run --project src/MapMe/MapMe.csproj

# Application will start at:
# HTTPS: https://localhost:8008
# HTTP: http://localhost:8007 (redirects to HTTPS)
```

#### Using IDE
- **JetBrains Rider**: Open `MapMe.sln` → Run "MapMe" configuration
- **Visual Studio**: Open `MapMe.sln` → Set MapMe as startup project → F5
- **VS Code**: Open folder → Run "Launch MapMe" configuration

### 5. Verification & Testing

#### Application Health Check
```bash
# Health check
curl https://localhost:8008/health
# Should return: Healthy

# Test Google Maps integration
# 1. Open: https://localhost:8008
# 2. Create account or sign in
# 3. Verify map loads correctly
# 4. Test location search functionality
```

#### Run Test Suite
```bash
# Run all tests
./scripts/test-all.sh

# Run specific test categories
./scripts/test-unit.sh
./scripts/test-integration.sh

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Development Workflow

### Daily Development Process

```bash
# 1. Start development session
dotnet run --project src/MapMe/MapMe.csproj

# 2. In another terminal - watch for changes
dotnet watch run --project src/MapMe/MapMe.csproj

# 3. Run tests before making changes
./scripts/test-all.sh
```

### Feature Development Workflow

1. **Pull latest changes**: `git pull origin main`
2. **Create feature branch**: `git checkout -b feature/your-feature-name`
3. **Make changes**: Edit code with hot reload support
4. **Run tests**: `./scripts/test-all.sh` to verify changes
5. **Test manually**: Verify functionality in browser
6. **Commit with configured hooks**: Git commit triggers all quality checks
7. **Push and create PR**: Follow contribution guidelines

## Alternative Development Environments

### Visual Studio 2022 + GitHub Copilot

- Windows-optimized development
- Integrated Azure tools
- GitHub Copilot integration

### VS Code + C# Dev Kit

- Lightweight cross-platform option
- Extensive extension ecosystem
- Good for frontend development

**Essential Extensions (Any IDE):**

- SonarQube for IDE
- GitToolBox
- .env files support
- Database Tools
- HTTP Client
- Docker support

## Common Development Tasks

### Adding New Features

1. **Backend**: Add models in `src/MapMe/Models/`, services in `src/MapMe/Services/`
2. **API**: Add endpoints in `src/MapMe/Program.cs` (minimal APIs)
3. **Frontend**: Add Blazor components in `src/MapMe.Client/Pages/` or `Components/`
4. **Tests**: Add unit tests in `src/MapMe.Tests/Unit/`, integration in `Integration/`
5. **Documentation**: Update relevant docs in `docs/` folder

### Key File Locations
```bash
# Backend structure
src/MapMe/
├── Models/          # Data models
├── Services/        # Business logic
├── Repositories/    # Data access
├── Authentication/  # JWT auth
└── Program.cs       # API endpoints

# Frontend structure
src/MapMe.Client/
├── Pages/           # Blazor pages
├── Components/      # Reusable components
├── Services/        # Client services
└── wwwroot/js/      # JavaScript integration
```

## Troubleshooting

### Common Issues & Solutions

| Issue                       | Solution                                                          |
|-----------------------------|-------------------------------------------------------------------|
| **Port already in use**     | `lsof -i :8008` → `kill -9 <PID>` or update `launchSettings.json` |
| **Google Maps not loading** | Verify API key: `dotnet user-secrets list`, check browser console |
| **Build errors**            | `dotnet clean && dotnet restore && dotnet build`                  |
| **Authentication issues**   | Clear browser storage, check JWT config, verify user secrets      |
| **.NET 10 not found**       | Download from Microsoft .NET downloads page                       |
| **GPG signing fails**       | `gpg --list-secret-keys`, verify `git config user.signingkey`     |
| **Rider performance**       | Increase heap size, disable unnecessary plugins, clear caches     |

### Getting Help

- **Documentation**: Check `docs/troubleshooting/README.md`
- **GitHub Issues**: Search existing issues or create new one
- **Code Review**: Request help during code review process

## Performance & Debugging Tips

### Development Performance
- **Hot Reload**: Use `dotnet watch` for automatic recompilation
- **Parallel Testing**: Run `./scripts/test-all.sh` for faster feedback
- **IDE Optimization**: Configure Rider memory settings for large solutions
- **Build Optimization**: Enable parallel builds in IDE settings

### Debugging Best Practices

- **Structured Logging**: Use Serilog with SecureLogging utilities (see `docs/security/secure-logging.md`)
- **Breakpoint Debugging**: Set breakpoints in both C# and JavaScript
- **Network Inspection**: Use browser dev tools for API calls
- **Database Queries**: Monitor Cosmos DB query performance

## Security Considerations

- **Never commit real secrets**: Use User Secrets for development
- **Use secure logging**: Follow `docs/security/secure-logging.md` guidelines
- **GPG sign commits**: Verify commit authenticity
- **Keep dependencies updated**: Regular security updates

## Next Steps

After successful setup:

1. **Read Architecture**: Review `docs/architecture/README.md`
2. **Make First Change**: Try a small feature following `docs/getting-started/first-contribution.md`
3. **Follow Coding Standards**: Review `docs/development/ai-coding-assistant-rulebook.md`
4. **Contribute**: Follow `CONTRIBUTING.md` guidelines

---

**Estimated Setup Time**: 45-60 minutes  
**Last Updated**: 2025-09-03  
**Single Source of Truth**: This guide replaces all other local development documentation
