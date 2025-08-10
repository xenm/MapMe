# Deployment Guide

Targets
- Local: development HTTPS on https://localhost:8008
- Cloud/container: any platform supporting ASP.NET Core

Build
- Release build: dotnet publish MapMe/MapMe/MapMe.csproj -c Release -o out
- Artifacts: self-contained or framework-dependent depending on your needs

Environment configuration
- Set environment variables on the host/container:
  - ASPNETCORE_ENVIRONMENT=Production
  - GOOGLE_MAPS_API_KEY=your-key
- Optional: configure URLs via ASPNETCORE_URLS

Docker (example)
- Use a multi-stage Dockerfile (suggested) with the SDK image for build and ASP.NET runtime for serve
- Copy published output into the runtime image

Reverse proxy
- When hosting behind Nginx/IIS/Apache, ensure HTTPS and correct forwarded headers
- Configure compression and caching for static files

Monitoring & logs
- Enable structured logging
- Forward logs to your platform (CloudWatch, ELK, etc.)

Migrations & data
- Not applicable unless you add a database; document your process here if you do
