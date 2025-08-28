# Frontend Integration Guide

This document outlines how to integrate additional frontend frameworks with the MapMe project while maintaining proper dependency management and security monitoring.

## Current Architecture

MapMe currently uses a **Blazor Server + WebAssembly hybrid** architecture:
- **Server**: ASP.NET Core Blazor Server (MapMe project)
- **Client**: Blazor WebAssembly (MapMe.Client project)
- **JavaScript**: CDN-based dependencies + custom code

## Adding Frontend Frameworks

### Option 1: React/Vue/Angular/Next.js (npm ecosystem)

#### Setup Steps:
1. **Create frontend directory structure:**
   ```bash
   mkdir frontend
   cd frontend
   npm init -y
   ```

2. **Update Dependabot configuration:**
   ```yaml
   # Uncomment in .github/dependabot.yml:
   - package-ecosystem: "npm"
     directory: "/frontend"
     schedule:
       interval: "daily"
     open-pull-requests-limit: 10
     labels:
       - "dependencies"
       - "javascript"
       - "frontend"
   ```

3. **Update .gitignore:**
   ```gitignore
   # Frontend dependencies
   frontend/node_modules/
   frontend/dist/
   frontend/.env.local
   frontend/.next/
   frontend/build/
   ```

4. **Integration approaches:**
   - **Separate SPA**: Serve from different port/subdomain
   - **Static build**: Build frontend and serve from ASP.NET Core
   - **Micro-frontend**: Embed components in Blazor pages

#### Recommended Project Structure:
```
MapMe/
├── frontend/                    # React/Vue/Angular app
│   ├── package.json
│   ├── src/
│   └── public/
├── MapMe/MapMe/                # ASP.NET Core server
├── MapMe/MapMe.Client/         # Blazor WebAssembly
└── MapMe/MapMe.Tests/          # Tests
```

### Option 2: Flutter Mobile App (pub ecosystem)

#### Setup Steps:
1. **Create Flutter app:**
   ```bash
   flutter create mobile
   cd mobile
   ```

2. **Update Dependabot configuration:**
   ```yaml
   # Uncomment in .github/dependabot.yml:
   - package-ecosystem: "pub"
     directory: "/mobile"
     schedule:
       interval: "daily"
     open-pull-requests-limit: 10
     labels:
       - "dependencies"
       - "dart"
       - "flutter"
       - "mobile"
   ```

3. **Update .gitignore:**
   ```gitignore
   # Flutter
   mobile/.dart_tool/
   mobile/.flutter-plugins
   mobile/.flutter-plugins-dependencies
   mobile/.packages
   mobile/.pub-cache/
   mobile/.pub/
   mobile/build/
   mobile/ios/Pods/
   mobile/ios/.symlinks/
   mobile/android/.gradle/
   mobile/android/captures/
   mobile/android/gradlew
   mobile/android/gradlew.bat
   mobile/android/local.properties
   mobile/android/**/GeneratedPluginRegistrant.java
   mobile/android/key.properties
   ```

#### Recommended Project Structure:
```
MapMe/
├── mobile/                     # Flutter mobile app
│   ├── pubspec.yaml
│   ├── lib/
│   ├── android/
│   └── ios/
├── MapMe/MapMe/               # ASP.NET Core server
├── MapMe/MapMe.Client/        # Blazor WebAssembly
└── MapMe/MapMe.Tests/         # Tests
```

## CDN Dependency Management

### Current CDN Dependencies:
- **Google Maps API**: `https://maps.googleapis.com/maps/api/js`
- **Blazor Bootstrap**: Managed via NuGet (not CDN)

### CDN Security Monitoring Options:

#### Option 1: Manual Monitoring
- **Google Maps API**: Monitor Google's release notes
- **Documentation**: Keep track of API version changes
- **Security**: Google handles security updates automatically

#### Option 2: Automated CDN Monitoring Tools

##### Snyk (Recommended)
```yaml
# Add to .github/workflows/security.yml
name: Security Scan
on:
  schedule:
    - cron: '0 2 * * 1'  # Weekly Monday 2 AM
  push:
    branches: [main]

jobs:
  snyk:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run Snyk to check for vulnerabilities
        uses: snyk/actions/node@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
        with:
          args: --file=MapMe/MapMe.Client/wwwroot/js/mapInitializer.js
```

##### Dependabot Alternatives for CDN
```yaml
# Custom GitHub Action for CDN monitoring
name: CDN Dependency Check
on:
  schedule:
    - cron: '0 6 * * *'  # Daily at 6 AM

jobs:
  cdn-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Check Google Maps API
        run: |
          # Custom script to check for Google Maps API updates
          curl -s "https://developers.google.com/maps/documentation/javascript/releases" | \
          grep -o "version=[0-9.]*" | head -1
```

#### Option 3: Subresource Integrity (SRI)
```html
<!-- Add to CDN script tags for security -->
<script src="https://maps.googleapis.com/maps/api/js?key=API_KEY" 
        integrity="sha384-HASH_VALUE" 
        crossorigin="anonymous"></script>
```

### Recommended CDN Strategy:
1. **Keep current CDN approach** for Google Maps (runtime API key loading)
2. **Add SRI hashes** when possible for security
3. **Use Snyk or similar** for automated vulnerability scanning
4. **Monitor Google's release notes** manually for major updates
5. **Consider npm alternatives** only if CDN becomes problematic

## Migration Strategies

### Gradual Migration:
1. **Phase 1**: Keep current Blazor architecture
2. **Phase 2**: Add new frontend framework alongside
3. **Phase 3**: Gradually migrate components
4. **Phase 4**: Deprecate old components if needed

### API Integration:
- **Shared API**: Use existing ASP.NET Core API endpoints
- **Authentication**: Extend current session-based auth
- **Real-time**: Consider SignalR for live features

## Dependabot Configuration Management

The current `.github/dependabot.yml` is prepared for future expansion:
- **npm**: Ready to uncomment when adding React/Vue/Angular
- **pub**: Ready to uncomment when adding Flutter
- **Docker**: Already active for containerization
- **NuGet**: Already active for .NET dependencies
- **GitHub Actions**: Already active for workflow monitoring

## Security Considerations

### Frontend Security:
- **API Keys**: Never expose in frontend code
- **CORS**: Configure properly for new origins
- **CSP**: Update Content Security Policy
- **Authentication**: Extend current auth system

### Dependency Security:
- **Dependabot**: Automated vulnerability scanning
- **Snyk**: Additional security monitoring
- **Regular Updates**: Keep dependencies current
- **Security Audits**: Regular npm audit / flutter pub deps

## Conclusion

The MapMe project is well-prepared for frontend framework integration:
- **Dependabot configuration**: Ready for npm and pub ecosystems
- **Project structure**: Supports multiple frontend approaches
- **Security**: Multiple monitoring strategies available
- **Migration**: Gradual integration possible without breaking changes

Choose the integration approach based on your specific requirements:
- **Mobile-first**: Flutter + existing Blazor web
- **Modern web**: React/Vue/Angular + ASP.NET Core API
- **Hybrid**: Keep Blazor + add specific framework components
