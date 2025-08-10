# Security and Secrets Management

Principles
- Never commit secrets to source control
- Prefer System.Text.Json and .NET 10 best practices
- Restrict third-party API keys by origin and scope

Google Maps Key
- Configure via User Secrets (Development) or environment variable (Prod): see configuration.md
- Endpoint /config/maps provides the key to the client at runtime

CORS and origins
- If adding API controllers, configure CORS policies appropriately in Program.cs

Headers & HTTPS
- Enforce HTTPS redirection and HSTS in production
- Add security headers if exposing APIs publicly (e.g., CSP, X-Content-Type-Options)

Data privacy
- Avoid logging sensitive user information
- If storing user data, ensure proper consent and data retention policies

Dependencies
- Keep NuGet packages up to date
- Use GitHub Dependabot/automated scanning where possible

Auth (future)
- Consider integrating ASP.NET Core Identity or external auth providers
- Store tokens securely and never in client-side source
