# Local Development Setup

Prerequisites
- .NET 10 SDK installed
- Modern browser (Chrome/Edge/Firefox/Safari)

Clone and run
- git clone <repo>
- cd MapMe
- Configure Google Maps API key (see configuration.md)
- Run the server:
  - IDE: Run the "MapMe" project
  - CLI: dotnet run --project MapMe/MapMe/MapMe.csproj

Default URLs
- HTTPS: https://localhost:8008

Developer tips
- Hot reload works for Razor and most C# changes
- For JS changes in wwwroot/js, refresh the browser
- Use MapMe.debugRenderMockMarks() in the browser console to load mock marks

Troubleshooting
- If port in use, stop other dev instances or update launchSettings.json
- If Maps does not load, verify your API key and referer restrictions

