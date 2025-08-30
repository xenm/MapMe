# Backend Configuration

## Configuration and Secrets Management

### Configuration Structure

The MapMe backend uses standard ASP.NET Core configuration with the following structure:

**appsettings.json** (Production baseline):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**appsettings.Development.json** (Development template):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Azure.Cosmos": "Information"
    }
  },
  "AllowedHosts": "*",
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY_HERE"
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID_HERE"
  },
  "Jwt": {
    "SecretKey": "YOUR_JWT_KEY_32+_CHARS",
    "Issuer": "MapMe",
    "Audience": "MapMe",
    "ExpirationHours": 24
  }
}
```

### Google Maps API Key Configuration

**Source of truth is server configuration.**

**Lookup order:**
1. Configuration key: `GoogleMaps:ApiKey` (includes User Secrets in Development)
2. Environment variable: `GOOGLE_MAPS_API_KEY`

### Local Development (User Secrets - Recommended)

From `MapMe/MapMe/MapMe`:
```bash
dotnet user-secrets init
dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-api-key"
```

### Environment Variables

Set `GOOGLE_MAPS_API_KEY` in your shell or container environment:
```bash
export GOOGLE_MAPS_API_KEY="your-google-api-key"
```

### Server API Endpoint

- **GET /config/maps** → Returns the configured API key to the client at runtime
- **Implementation**: Located in `MapMe/Program.cs` as minimal API endpoint
- **Usage**: Called by `Map.razor` during component initialization

### JWT Configuration

**Required Configuration:**
```json
{
  "Jwt": {
    "SecretKey": "your-jwt-secret-key-minimum-32-characters",
    "ExpirationHours": 24,
    "Issuer": "MapMe",
    "Audience": "MapMe"
  }
}
```

**User Secrets Setup:**
```bash
dotnet user-secrets set "Jwt:SecretKey" "your-secure-jwt-secret-key-here"
```

### Google OAuth Configuration

**Required Configuration:**
```json
{
  "Google": {
    "ClientId": "your-google-oauth-client-id"
  }
}
```

**User Secrets Setup:**
```bash
dotnet user-secrets set "Google:ClientId" "your-google-oauth-client-id"
```

## JSON Serialization Configuration

### System.Text.Json Implementation

MapMe uses System.Text.Json exclusively throughout the application for consistent serialization behavior.

**Project Configuration:**
```xml
<!-- Disable Cosmos DB Newtonsoft.Json requirement -->
<AzureCosmosDisableNewtonsoftJsonCheck>true</AzureCosmosDisableNewtonsoftJsonCheck>
```

**Default Serialization Options:**
```csharp
private static readonly JsonSerializerOptions DefaultOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

### Custom Cosmos DB Serializer

**SystemTextJsonCosmosSerializer Class:**
```csharp
public class SystemTextJsonCosmosSerializer : CosmosSerializer
{
    // Provides full compatibility with Azure Cosmos DB operations
    // Uses System.Text.Json instead of Newtonsoft.Json
    // Ensures consistent JSON handling throughout the stack
}
```

**Benefits:**
- **Performance**: Better memory efficiency and faster serialization
- **Security**: Eliminates vulnerable dependencies from legacy JSON libraries
- **Consistency**: Single JSON library across entire application stack

### Database Configuration

**Cosmos DB Configuration:**
```json
{
  "CosmosDb": {
    "ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=...",
    "DatabaseName": "mapme",
    "ContainerNames": {
      "UserProfiles": "UserProfiles",
      "DateMarks": "DateMarks",
      "ChatMessages": "ChatMessages",
      "Conversations": "Conversations"
    }
  }
}
```

**Development with Cosmos DB Emulator:**
```bash
# Start Cosmos DB Emulator
./Scripts/start-cosmos.ps1

# Configure connection string
dotnet user-secrets set "CosmosDb:ConnectionString" "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw=="
```

### Security Best Practices

**Configuration Security:**
- Do not commit secrets to source control or `launchSettings.json`
- Restrict the Google Maps API key in Google Cloud Console (HTTP referrers, API restrictions)
- Use User Secrets for local development
- Use Azure Key Vault or similar for production secrets
- Rotate JWT secret keys regularly

**Environment-Specific Configuration:**
- **Development**: User Secrets + appsettings.Development.json
- **Staging**: Environment variables + Azure App Configuration
- **Production**: Azure Key Vault + environment variables

### Configuration Validation

The application validates configuration on startup:
- Google Maps API key presence
- JWT secret key minimum length (32 characters)
- Database connection string format
- Required configuration sections

### Troubleshooting Configuration Issues

**Common Issues:**
- Missing Google Maps API key → Check User Secrets and environment variables
- JWT authentication failures → Verify secret key length and format
- Database connection failures → Validate Cosmos DB emulator or connection string
- Configuration not loading → Check appsettings.json hierarchy and User Secrets

---

**Related Documentation:**
- [Backend Overview](README.md)
- [Authentication](../security/authentication.md)
- [Database Setup](data-access.md)
- [Local Development](../getting-started/local-development.md)
