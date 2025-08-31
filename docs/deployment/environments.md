# Environment Configuration

## Deployment Targets

### Local Development
- **Target**: Development HTTPS on `https://localhost:8008`
- **Environment**: `ASPNETCORE_ENVIRONMENT=Development`
- **Configuration**: User Secrets + appsettings.Development.json

### Staging Environment
- **Target**: Cloud/container platform supporting ASP.NET Core
- **Environment**: `ASPNETCORE_ENVIRONMENT=Staging`
- **Configuration**: Environment variables + Azure App Configuration

### Production Environment
- **Target**: Cloud/container platform with high availability
- **Environment**: `ASPNETCORE_ENVIRONMENT=Production`
- **Configuration**: Environment variables + Azure Key Vault

## Environment Variables

### Required Configuration

**Core Application:**
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
```

**Google Maps Integration:**
```bash
GOOGLE_MAPS_API_KEY=your-google-maps-api-key
```

**JWT Authentication:**
```bash
JWT_SECRET_KEY=your-jwt-secret-key-minimum-32-characters
JWT_EXPIRATION_HOURS=24
JWT_ISSUER=MapMe
JWT_AUDIENCE=MapMe-Users
```

**Database Configuration:**
```bash
COSMOSDB_CONNECTION_STRING=AccountEndpoint=https://...
COSMOSDB_DATABASE_NAME=mapme
```

### Optional Configuration

**URLs and Ports:**
```bash
ASPNETCORE_URLS=https://+:8008;http://+:8080
```

**Logging:**
```bash
SERILOG_MINIMUM_LEVEL=Information
SERILOG_WRITE_TO_CONSOLE=true
```

## Configuration Hierarchy

### Development
1. User Secrets (highest priority)
2. appsettings.Development.json
3. appsettings.json
4. Environment variables (lowest priority)

### Staging/Production
1. Environment variables (highest priority)
2. Azure Key Vault / Secret management
3. Azure App Configuration
4. appsettings.json (lowest priority)

## Security Considerations

### Secret Management
- **Development**: Use User Secrets for local development
- **Staging/Production**: Use Azure Key Vault or equivalent secret management
- **Never**: Commit secrets to source control or configuration files

### API Key Restrictions
- Restrict Google Maps API key by:
  - HTTP referrers (domain restrictions)
  - API restrictions (Maps JavaScript API, Places API only)
  - Usage quotas and billing alerts

### HTTPS Configuration
- **Development**: Self-signed certificates via `dotnet dev-certs https`
- **Production**: Valid SSL certificates from trusted CA
- **HSTS**: Enable HTTP Strict Transport Security headers

## Health Checks

### Application Health
- `/health` endpoint for basic application health
- `/health/ready` endpoint for readiness probes
- `/health/live` endpoint for liveness probes

### Dependency Health
- Google Maps API connectivity
- Cosmos DB connection and query capability
- JWT configuration validation

## Environment-Specific Features

### Development
- Detailed error pages
- Developer exception page
- Hot reload and file watching
- Swagger/OpenAPI documentation

### Staging
- Production-like configuration
- Limited error details
- Performance monitoring
- Load testing capabilities

### Production
- Minimal error exposure
- Comprehensive logging and monitoring
- Performance optimization
- Security hardening

---

**Related Documentation:**
- [Deployment Overview](README.md)
- [Infrastructure](infrastructure.md)
- [CI/CD Pipeline](ci-cd.md)
- [Backend Configuration](../backend/configuration.md)
