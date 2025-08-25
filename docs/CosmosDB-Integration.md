# CosmosDB Integration for MapMe

This document describes the complete CosmosDB integration for the MapMe dating application, including setup, configuration, and usage.

## Overview

MapMe now supports both in-memory repositories (for development/testing) and CosmosDB repositories (for production) with automatic fallback based on configuration.

## Architecture

### Repository Pattern
- **Base Class**: `CosmosRepositoryBase<T>` provides common CosmosDB operations
- **User Profiles**: `CosmosUserProfileRepository` handles user profile data
- **DateMarks**: `CosmosDateMarkByUserRepository` handles location-based dating data with geospatial queries

### Containers Structure
- **UserProfiles**: User profile data with user-based partitioning
- **DateMarks**: Location and dating preference data with geospatial indexing
- **ChatMessages**: Chat messages (future implementation)
- **Conversations**: Chat conversations (future implementation)

## Quick Start

### 1. Start CosmosDB Emulator
```powershell
# Start emulator and initialize database
./Scripts/start-cosmos.ps1

# Or start without initialization
./Scripts/start-cosmos.ps1 -SkipInit
```

### 2. Configure Application
```bash
# Copy sample configuration
cp MapMe/MapMe/appsettings.Development.sample.json MapMe/MapMe/appsettings.Development.json

# Edit configuration as needed
```

### 3. Connect Storage Explorer
- **Endpoint**: `https://localhost:8081`
- **Key**: `C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw==`
- **Database**: `mapme`

### 4. Run Application
```bash
dotnet run --project MapMe/MapMe
```

## Configuration

### appsettings.Development.json
```json
{
  "Cosmos": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw==",
    "Database": "mapme"
  }
}
```

### Production Configuration
```json
{
  "Cosmos": {
    "Endpoint": "https://your-cosmos-account.documents.azure.com:443/",
    "Key": "your-production-key",
    "Database": "mapme-prod"
  }
}
```

## Container Details

### UserProfiles Container
- **Partition Key**: `/id`
- **Indexing**: Optimized for user queries
- **Throughput**: 400 RU/s (development)

**Sample Document**:
```json
{
  "id": "user-123",
  "userId": "user-123",
  "displayName": "John Doe",
  "bio": "Love exploring new places!",
  "photos": [...],
  "preferences": {...},
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### DateMarks Container
- **Partition Key**: `/userId`
- **Geospatial Indexing**: `/location/*` for radius queries
- **Composite Indexes**: userId + visitDate, userId + createdAt
- **Throughput**: 400 RU/s (development)

**Sample Document**:
```json
{
  "id": "datemark-456",
  "userId": "user-123",
  "placeName": "Central Park",
  "location": {
    "type": "Point",
    "coordinates": [-73.965355, 40.782865]
  },
  "categories": ["Parks", "Recreation"],
  "tags": ["romantic", "peaceful"],
  "qualities": ["scenic", "walkable"],
  "visitDate": "2024-01-15",
  "rating": 5,
  "wouldRecommend": true,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

## Geospatial Queries

### Radius Search
The `CosmosDateMarkByUserRepository` supports geospatial queries using CosmosDB's `ST_DISTANCE` function:

```csharp
await foreach (var dateMark in repository.GetByLocationAsync(
    latitude: 40.7829, 
    longitude: -73.9654, 
    radiusMeters: 1000))
{
    // Process nearby DateMarks
}
```

### SQL Query Example
```sql
SELECT * FROM c 
WHERE ST_DISTANCE(c.location, {
    'type': 'Point', 
    'coordinates': [-73.9654, 40.7829]
}) <= 1000
AND (NOT IS_DEFINED(c.isDeleted) OR c.isDeleted = false)
```

## Development Scripts

### Start CosmosDB
```powershell
# Full setup (start + initialize)
./Scripts/start-cosmos.ps1

# Start only (skip initialization)
./Scripts/start-cosmos.ps1 -SkipInit

# Run detached
./Scripts/start-cosmos.ps1 -Detached
```

### Initialize Database
```powershell
# Initialize with default settings
./Scripts/init-cosmosdb.ps1

# Initialize with custom settings
./Scripts/init-cosmosdb.ps1 -Endpoint "https://custom-endpoint" -Key "custom-key" -DatabaseName "custom-db"
```

### Stop CosmosDB
```powershell
# Stop and remove container
./Scripts/stop-cosmos.ps1

# Stop but keep container (preserve data)
./Scripts/stop-cosmos.ps1 -KeepData
```

## Docker Compose Alternative

### Start with Docker Compose
```bash
# Start CosmosDB emulator
docker-compose -f docker-compose.cosmos.yml up -d

# View logs
docker-compose -f docker-compose.cosmos.yml logs -f

# Stop
docker-compose -f docker-compose.cosmos.yml down
```

## Storage Explorer Integration

### Connection Steps
1. Open **Azure Storage Explorer**
2. Click **"Add an account"** → **"CosmosDB"**
3. Select **"CosmosDB Account"**
4. Enter connection details:
   - **Account Endpoint**: `https://localhost:8081`
   - **Account Key**: `C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw==`
5. Click **"Connect"**

### Browsing Data
- Navigate to **CosmosDB Account** → **mapme** → **Containers**
- View **UserProfiles**, **DateMarks**, **ChatMessages**, **Conversations**
- Query data using SQL syntax
- Monitor performance metrics

## Performance Optimization

### Indexing Strategy
- **Automatic Indexing**: Enabled for all containers
- **Composite Indexes**: Optimized for common query patterns
- **Geospatial Indexes**: Enabled for location-based queries
- **Excluded Paths**: Large binary data (photos) excluded from indexing

### Query Optimization
- Use partition key in queries when possible
- Leverage composite indexes for multi-field queries
- Use `ST_DISTANCE` for geospatial queries
- Implement proper pagination for large result sets

### Throughput Management
- **Development**: 400 RU/s per container
- **Production**: Scale based on usage patterns
- **Auto-scale**: Consider enabling for variable workloads

## Troubleshooting

### Common Issues

#### CosmosDB Emulator Won't Start
```bash
# Check Docker status
docker ps

# View emulator logs
docker logs mapme-cosmos-emulator

# Restart emulator
./Scripts/stop-cosmos.ps1
./Scripts/start-cosmos.ps1
```

#### Connection Refused
- Ensure emulator is running: `docker ps`
- Check port 8081 is not blocked
- Verify certificate trust (emulator uses self-signed cert)

#### Storage Explorer Connection Issues
- Use exact endpoint: `https://localhost:8081`
- Use exact key (no extra spaces)
- Trust self-signed certificate if prompted

#### Query Performance Issues
- Check if queries use partition key
- Verify composite indexes are created
- Monitor RU consumption in Storage Explorer

### Debugging Queries
```csharp
// Enable query metrics
var queryRequestOptions = new QueryRequestOptions
{
    PopulateIndexMetrics = true
};

// View query metrics in logs
```

## Migration from In-Memory

The application automatically detects CosmosDB configuration and switches from in-memory repositories:

```csharp
// Program.cs - Automatic repository selection
var useCosmos = !string.IsNullOrWhiteSpace(cosmosEndpoint) && 
                !string.IsNullOrWhiteSpace(cosmosKey);

if (useCosmos)
{
    // Use CosmosDB repositories
    builder.Services.AddSingleton<IUserProfileRepository, CosmosUserProfileRepository>();
    builder.Services.AddSingleton<IDateMarkByUserRepository, CosmosDateMarkByUserRepository>();
}
else
{
    // Fallback to in-memory repositories
    builder.Services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
    builder.Services.AddSingleton<IDateMarkByUserRepository, InMemoryDateMarkByUserRepository>();
}
```

## Production Deployment

### Azure CosmosDB Setup
1. Create CosmosDB account in Azure
2. Create database and containers using initialization script
3. Configure connection strings in production settings
4. Set up monitoring and alerts

### Security Considerations
- Use Azure Key Vault for connection strings
- Enable firewall rules for production
- Implement proper authentication and authorization
- Monitor access patterns and unusual activity

### Backup and Recovery
- Enable automatic backups in Azure CosmosDB
- Test restore procedures regularly
- Document recovery processes

## Future Enhancements

### Planned Features
- **Chat Integration**: Complete CosmosDB implementation for chat messages
- **Real-time Sync**: SignalR integration with change feed
- **Advanced Geospatial**: Polygon and multi-point support
- **Analytics**: Query optimization and usage analytics

### Performance Improvements
- **Caching Layer**: Redis integration for frequently accessed data
- **Connection Pooling**: Optimize CosmosDB client configuration
- **Batch Operations**: Bulk insert/update operations
- **Partitioning Strategy**: Optimize for scale

## Support

For issues or questions:
1. Check troubleshooting section above
2. Review CosmosDB emulator logs
3. Verify configuration settings
4. Test with Storage Explorer

## References

- [Azure CosmosDB Documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/)
- [CosmosDB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- [Geospatial Queries](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/sql-query-geospatial-intro)
- [Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/)
