# Cosmos DB Local Setup Guide

## Local Cosmos DB Emulator Connection Details

For local development with the Cosmos DB emulator, use these standard connection details:

### Connection Configuration
```json
{
  "CosmosDb": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "mapme"
  }
}
```

### Adding to Your appsettings.Development.json

Add the CosmosDb section to your `appsettings.Development.json`:

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
  },
  "CosmosDb": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "mapme"
  }
}
```

## Important Notes

### Standard Emulator Keys
- These are the **standard Microsoft Cosmos DB emulator keys**
- They are the same for all developers using the local emulator
- They are **only for local development** and are not secret

### Production Environment
- **Production requires real Azure Cosmos DB connection details**
- The application will **fail to start** in production without proper Cosmos DB configuration
- This is a security feature to prevent accidental use of in-memory repositories in production

### Environment Behavior
- **Development**: Uses in-memory repositories if Cosmos DB not configured (for testing)
- **Production**: **Requires** Cosmos DB configuration, fails startup if missing
- **Testing**: Always uses in-memory repositories regardless of configuration

## Starting Cosmos DB Emulator

Use the provided PowerShell script:
```bash
./Scripts/start-cosmos.ps1
```

Or start manually with Docker:
```bash
docker run -d --name mapme-cosmos-emulator \
  --platform linux/amd64 \
  -p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254 \
  -m 3g --cpus="2.0" \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=3 \
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true \
  -e AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

## Azure Storage Explorer Connection

Use these same connection details in Azure Storage Explorer:
- **Endpoint**: https://localhost:8081
- **Key**: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
- **Database**: mapme
