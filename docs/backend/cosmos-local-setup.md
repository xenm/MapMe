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

---

# Cosmos DB Pruning & Database Management

This section describes the Cosmos DB pruning scripts used to clean and reset the MapMe database for development and
testing purposes.

## Overview

The MapMe project includes comprehensive scripts to prune (clean/reset) the Cosmos DB database. These scripts are
essential for:

- **Fresh User Testing**: Clean database for testing Google login with new users
- **Development Reset**: Clear test data and start fresh during development
- **CI/CD Pipeline**: Reset database state between test runs
- **Debugging**: Isolate issues by starting with clean data

## Available Pruning Scripts

### Unix/macOS Script: `prune-cosmosdb.sh`

**Location**: `/scripts/prune-cosmosdb.sh`

**Usage**:

```bash
# Quick container cleanup (recommended)
./prune-cosmosdb.sh --containers-only --yes

# Full database reset
./prune-cosmosdb.sh --yes

# Nuclear option - restart emulator (clears everything)
./prune-cosmosdb.sh --restart-emulator --yes

# Interactive mode (with confirmations)
./prune-cosmosdb.sh --containers-only
```

### Windows PowerShell Script: `prune-cosmosdb.ps1`

**Location**: `/scripts/prune-cosmosdb.ps1`

**Usage**:

```powershell
# Quick container cleanup (recommended)
.\prune-cosmosdb.ps1 -ContainersOnly -Yes

# Full database reset
.\prune-cosmosdb.ps1 -Yes

# Nuclear option - restart emulator (clears everything)
.\prune-cosmosdb.ps1 -RestartEmulator -Yes

# Interactive mode (with confirmations)
.\prune-cosmosdb.ps1 -ContainersOnly
```

## Script Options

### Common Parameters

| Parameter                                 | Description                              | Default                  |
|-------------------------------------------|------------------------------------------|--------------------------|
| `--endpoint` / `-Endpoint`                | Cosmos DB endpoint URL                   | `https://localhost:8081` |
| `--key` / `-Key`                          | Cosmos DB authentication key             | Emulator default key     |
| `--database` / `-Database`                | Database name                            | `mapme`                  |
| `--containers-only` / `-ContainersOnly`   | Only delete containers, keep database    | `false`                  |
| `--restart-emulator` / `-RestartEmulator` | Restart entire emulator (nuclear option) | `false`                  |
| `--yes` / `-Yes`                          | Skip confirmation prompts                | `false`                  |
| `--help` / `-Help`                        | Show usage information                   | N/A                      |

### Pruning Modes (in order of severity)

1. **Containers Only** (`--containers-only`)
    - Deletes and recreates containers
    - Keeps database structure
    - **Fastest option** for routine cleanup
    - **Recommended** for development

2. **Full Database** (default)
    - Deletes entire database and recreates it
    - More thorough cleanup
    - Use when containers-only isn't sufficient

3. **Restart Emulator** (`--restart-emulator`)
    - **Nuclear option** - stops and restarts emulator
    - Clears ALL data in emulator
    - Use only when other options fail

## Database Structure

The scripts handle the following Cosmos DB containers:

| Container       | Partition Key     | Purpose                              |
|-----------------|-------------------|--------------------------------------|
| `UserProfiles`  | `/id`             | User profile data and preferences    |
| `DateMarks`     | `/userId`         | User's date marks and location data  |
| `ChatMessages`  | `/conversationId` | Chat messages between users          |
| `Conversations` | `/id`             | Conversation metadata                |
| `Users`         | `/id`             | Authentication and user account data |

## Complete Reset Workflow

For a complete fresh start (recommended after pruning):

### 1. Run Pruning Script

```bash
# Unix/macOS
./scripts/prune-cosmosdb.sh --containers-only --yes

# Windows
.\scripts\prune-cosmosdb.ps1 -ContainersOnly -Yes
```

### 2. Clear Browser Storage

Open browser Developer Tools (F12) and run:

```javascript
// Clear all MapMe data
localStorage.clear();
sessionStorage.clear();

// Or clear specific items
localStorage.removeItem('mapme_jwt_token');
localStorage.removeItem('userProfile');
localStorage.removeItem('dateMarks');
```

### 3. Restart Application

```bash
# Stop current application (Ctrl+C)
# Then restart
dotnet run --project src/MapMe
```

### 4. Test Fresh User Flow

1. Navigate to application
2. Sign up with Google as a new user
3. Verify redirect to Profile page
4. Complete profile setup
5. Test DateMark creation on Map

## Troubleshooting

### Common Issues

**Script Permission Denied (Unix/macOS)**

```bash
chmod +x scripts/prune-cosmosdb.sh
```

**Cosmos DB Not Accessible**

```bash
# Check if emulator is running
docker ps | grep cosmos

# Start emulator if needed
./scripts/start-cosmos.sh
```

**PowerShell Execution Policy (Windows)**

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Verification Steps

After running pruning scripts:

1. **Check Emulator Status**:
   ```bash
   curl -k https://localhost:8081
   ```

2. **Verify Container Recreation**:
    - Use Azure Storage Explorer
    - Connect to `https://localhost:8081` with emulator key
    - Verify `mapme` database and containers exist but are empty

3. **Test Application**:
    - Start application: `dotnet run --project src/MapMe`
    - Verify no existing user data
    - Test new user registration flow

## Integration with Development Workflow

### Daily Development

```bash
# Quick cleanup for testing
./scripts/prune-cosmosdb.sh --containers-only --yes
```

### Feature Testing

```bash
# Full reset for comprehensive testing
./scripts/prune-cosmosdb.sh --yes
```

### CI/CD Pipeline

```bash
# Automated testing setup
./scripts/prune-cosmosdb.sh --containers-only --yes
# Run tests
dotnet test
```

### Debugging Issues

```bash
# Nuclear option when other methods fail
./scripts/prune-cosmosdb.sh --restart-emulator --yes
```

## Security Considerations

- **Development Only**: These scripts are designed for development/testing
- **Production Warning**: Never run against production Cosmos DB
- **Data Loss**: All data will be permanently deleted
- **Confirmation**: Use `--yes` flag carefully in automated scripts
