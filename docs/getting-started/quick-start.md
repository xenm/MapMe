# Quick Start Guide

Get MapMe running in 5 minutes! This guide assumes you have the basic prerequisites installed.

## Prerequisites Check

Ensure you have:
- ✅ .NET 10 SDK (preview)
- ✅ Git
- ✅ Modern web browser
- ✅ Google Maps API key (see [Prerequisites](./prerequisites.md) for setup)

## 5-Minute Setup

### 1. Clone and Navigate
```bash
git clone https://github.com/xenm/MapMe.git
cd MapMe
```

### 2. Configure Google Maps API Key
```bash
cd MapMe/MapMe/MapMe
dotnet user-secrets init
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-api-key-here"
cd ../../..
```

### 3. Run the Application
```bash
dotnet run --project MapMe/MapMe/MapMe.csproj
```

### 4. Open in Browser
Navigate to: **https://localhost:8008**

## What You Should See

1. **Login/Signup Page**: Create an account or sign in
2. **Map Interface**: Interactive Google Maps with location search
3. **Profile Page**: Comprehensive user profile management

## Quick Test

1. **Create Date Mark**: Click anywhere on the map → Fill out the form → Save
2. **View Profile**: Navigate to `/profile` → See your saved Date Marks
3. **Edit Profile**: Click "Edit Profile" → Update your information

## Next Steps

- **Full Setup**: Follow [Local Development](./local-development.md) for complete setup
- **Understanding**: Read [Architecture Overview](../architecture/system-overview.md)
- **Contributing**: See [First Contribution](./first-contribution.md)

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Port already in use | Kill processes on ports 5260/7160 or update `launchSettings.json` |
| Google Maps not loading | Verify API key configuration and enabled APIs |
| Build errors | Run `dotnet restore` and check .NET 10 SDK installation |

## Support

- **Detailed Setup**: [Local Development](./local-development.md)
- **Common Issues**: [Troubleshooting Guide](../troubleshooting/README.md)
- **API Setup**: [Prerequisites](./prerequisites.md)

---

**Estimated Time**: 5 minutes  
**Last Updated**: 2025-08-30
