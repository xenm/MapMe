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
    "Issuer": "MapMe-Server",
    "Audience": "MapMe-Client",
    "ExpirationHours": 24
  },
  "CosmosDb": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "mapme"
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

## Cosmos DB Configuration

### Repository Selection Logic

The application automatically selects the appropriate data repositories based on environment and configuration:

- **Development Environment**: Uses Cosmos DB if configured, otherwise falls back to in-memory repositories
- **Production Environment**: **Requires** Cosmos DB configuration, fails startup if missing
- **Test Environment**: Always uses in-memory repositories regardless of configuration

### Local Development (Cosmos DB Emulator)

For local development, use the standard Cosmos DB emulator connection details:

```json
{
  "CosmosDb": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "mapme"
  }
}
```

**Note**: These are the standard Microsoft Cosmos DB emulator keys, identical for all developers.

### Production Environment

Production requires real Azure Cosmos DB connection details. The application will **fail to start** if Cosmos DB is not properly configured in production.

Use User Secrets or environment variables for production:
```bash
dotnet user-secrets set "CosmosDb:Endpoint" "https://your-cosmos.documents.azure.com:443/"
dotnet user-secrets set "CosmosDb:Key" "your-primary-key"
dotnet user-secrets set "CosmosDb:DatabaseName" "mapme"
```

### Configuration Validation

The application validates Cosmos DB configuration at startup:
- Rejects placeholder values (e.g., "YOUR_COSMOS_DB_ENDPOINT_HERE")
- Logs repository selection for debugging
- Enforces production safety requirements

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
    "Issuer": "MapMe-Server",
    "Audience": "MapMe-Client"
  }
}
```

**Configuration Rationale:**
MapMe uses a **client-server architecture** with ASP.NET Core Web/API server and Blazor WebAssembly client. The JWT configuration provides proper security separation:

- **Issuer (`"MapMe-Server"`)**: Identifies the ASP.NET Core server that issues JWT tokens (authentication service)
- **Audience (`"MapMe-Client"`)**: Identifies the intended recipient - the Blazor WebAssembly client application
- **Security Separation**: Clear distinction between token issuer and consumer for enhanced security

**Security Considerations:**
- **Token Scope Validation**: Tokens are explicitly scoped for the Blazor WebAssembly client
- **Architecture Boundaries**: Distinct issuer/audience enables proper token validation and security boundaries
- **Future Scalability**: Easy to add mobile apps, admin panels, or additional API services with different audiences
- **Best Practices**: Follows JWT RFC 7519 recommendations for client-server architectures

**Alternative Configurations:**
```json
// For multiple client applications
{
  "Issuer": "MapMe-Server",
  "Audience": "MapMe-Mobile-App"
}

// For admin or management interfaces
{
  "Issuer": "MapMe-Server", 
  "Audience": "MapMe-Admin-Panel"
}

// For external API consumers (future consideration)
{
  "Issuer": "MapMe-Server",
  "Audience": "MapMe-External-API"
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
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "mapme",
    "EnableSSLValidation": false
  }
}
```

**Development with Cosmos DB Emulator:**
```bash
# Start Cosmos DB Emulator
./Scripts/start-cosmos.ps1

# Configure connection details
dotnet user-secrets set "CosmosDb:Endpoint" "https://localhost:8081"
dotnet user-secrets set "CosmosDb:Key" "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
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
