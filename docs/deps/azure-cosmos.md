# Azure Cosmos DB (Dependency Setup)

Configure and use Azure Cosmos DB (or the emulator) with MapMe.

## Overview
MapMe supports in-memory repositories by default and can use Cosmos DB for production. Configuration controls which repository is active.

## Quick Start (Emulator)

### Start emulator and initialize database
```powershell
./Scripts/start-cosmos.ps1
# Or skip initialization
./Scripts/start-cosmos.ps1 -SkipInit
```

### Configure application
Copy and edit development settings:
```bash
cp MapMe/MapMe/appsettings.Development.sample.json MapMe/MapMe/appsettings.Development.json
```

### Storage Explorer
- Endpoint: https://localhost:8081
- Key: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw==
- Database: mapme

## Configuration

Development (emulator):
```json
{
  "Cosmos": {
    "Endpoint": "https://localhost:8081",
    "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw==",
    "Database": "mapme"
  }
}
```

Production:
```json
{
  "Cosmos": {
    "Endpoint": "https://your-cosmos-account.documents.azure.com:443/",
    "Key": "your-production-key",
    "Database": "mapme-prod"
  }
}
```

## Containers
- UserProfiles (partition key: `/id`)
- DateMarks (partition key: `/userId`, geospatial index on `/location/*`)
- ChatMessages (future)
- Conversations (future)

## Geospatial queries
Example C# radius query pattern using `ST_DISTANCE` under the hood:
```csharp
await foreach (var dateMark in repository.GetByLocationAsync(
    latitude: 40.7829,
    longitude: -73.9654,
    radiusMeters: 1000))
{
    // Process nearby items
}
```

SQL example:
```sql
SELECT * FROM c
WHERE ST_DISTANCE(c.location, {
  'type': 'Point',
  'coordinates': [-73.9654, 40.7829]
}) <= 1000
AND (NOT IS_DEFINED(c.isDeleted) OR c.isDeleted = false)
```

## Scripts
- start: `./Scripts/start-cosmos.ps1`
- init: `./Scripts/init-cosmosdb.ps1`
- stop: `./Scripts/stop-cosmos.ps1`
- docker compose alternative: `docker-compose -f docker-compose.cosmos.yml up -d`

## System.Text.Json Serializer
- Custom `SystemTextJsonCosmosSerializer` removes Newtonsoft.Json dependency and unifies JSON handling across server and client.
- Benefits: fewer dependencies, better perf, consistent options (camelCase, ignore nulls).
- Core methods:
  - `FromStream<T>(Stream stream)`: handles null/empty streams, returns default for empty content, supports raw Stream passthrough.
  - `ToStream<T>(T input)`: serializes with configured options, memory-efficient stream creation, proper positioning.

