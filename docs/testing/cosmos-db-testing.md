# Cosmos DB Testing Guide

This guide covers running service level tests against local Cosmos DB emulator for the MapMe application.

## Overview

The MapMe project includes comprehensive test scripts for running service level tests against a local Cosmos DB
emulator. This provides realistic testing scenarios with actual database operations, geospatial queries, and data
persistence.

## Prerequisites

### Required Software

- **Docker Desktop**: Latest version for running Cosmos DB emulator
- **.NET 10 SDK**: For building and running tests
- **PowerShell Core**: For cross-platform script execution
- **Azure Storage Explorer** (optional): For database inspection

### System Requirements

- **macOS**: M1/M2/M3 chips supported via Linux-based emulator
- **Windows**: Native Windows emulator or Linux-based via WSL2
- **Linux**: Linux-based emulator
- **Memory**: Minimum 4GB RAM available for Docker
- **Disk Space**: 2GB for emulator image and data

## Quick Start

### 1. Start Cosmos DB Emulator

```bash
# Start emulator and initialize database (PowerShell)
./Scripts/start-cosmos.ps1

# Start emulator and initialize database (Shell - macOS compatible)
./Scripts/start-cosmos.sh

# Or using Docker Compose
docker-compose -f docker-compose.cosmos.yml up -d
```

### 2. Run Health Check

```bash
# Verify emulator is ready (PowerShell)
./Scripts/test-cosmos-health.ps1

# Verify emulator is ready (Shell - macOS compatible)
./Scripts/test-cosmos-health.sh --detailed

# Run detailed health check with auto-fix (PowerShell)
./Scripts/test-cosmos-health.ps1 -Detailed -Fix

# Run detailed health check with auto-fix (Shell)
./Scripts/test-cosmos-health.sh --detailed --fix
```

### 3. Run Service Level Tests

```bash
# Run all service level tests (PowerShell)
./Scripts/test-service-cosmos.ps1

# Run specific test category (PowerShell)
./Scripts/test-service-cosmos.ps1 -TestFilter "Integration"

# Run with verbose output (PowerShell)
./Scripts/test-service-cosmos.ps1 -Verbose

# Note: Service level tests require PowerShell Core (pwsh) for full functionality
# Install PowerShell Core for macOS: brew install --cask powershell
```

### 4. Run Complete Test Suite

```bash
# Run all tests (unit, integration, service level)
./Scripts/test-all-cosmos.ps1

# Run with health check and cleanup
./Scripts/test-all-cosmos.ps1 -HealthCheck -StopCosmosAfter
```

## Test Scripts Reference

### test-service-cosmos.ps1

**Purpose**: Runs service level tests against Cosmos DB emulator

**Key Features**:

- Automatic Cosmos DB emulator startup and initialization
- Comprehensive API endpoint testing
- Data persistence validation
- Geospatial query testing
- HTML report generation
- Environment variable configuration

**Parameters**:

```powershell
./Scripts/test-service-cosmos.ps1 [options]

-SkipCosmosStart     # Skip starting emulator (assume running)
-TestFilter <string> # Filter tests by name pattern
-OutputDir <string>  # Test results directory (default: TestResults)
-NoHtml             # Skip HTML report generation
-Verbose            # Enable verbose output
-StopCosmosAfter    # Stop emulator after tests
```

**Examples**:

```bash
# Basic run
./Scripts/test-service-cosmos.ps1

# Run specific integration tests with verbose output
./Scripts/test-service-cosmos.ps1 -TestFilter "Integration" -Verbose

# Run without starting emulator, generate reports
./Scripts/test-service-cosmos.ps1 -SkipCosmosStart -OutputDir "MyResults"
```

### test-integration-cosmos.ps1

**Purpose**: Focused integration tests for Cosmos DB scenarios

**Key Features**:

- Cosmos DB readiness verification
- Integration test execution with real database
- Category-specific test filtering
- Coverage reporting

**Parameters**:

```powershell
./Scripts/test-integration-cosmos.ps1 [options]

-TestCategory <string> # Specific category (Integration, ChatApi, JwtApi)
-OutputDir <string>    # Results directory
-NoHtml               # Skip HTML reports
-Verbose              # Verbose output
```

### test-cosmos-health.ps1

**Purpose**: Comprehensive health check for Cosmos DB setup

**Key Features**:

- Docker status verification
- Container health monitoring
- Endpoint connectivity testing
- Database/container existence validation
- Automatic issue resolution

**Parameters**:

```powershell
./Scripts/test-cosmos-health.ps1 [options]

-Detailed  # Show detailed information and logs
-Fix       # Attempt automatic issue resolution
```

**Health Checks Performed**:

1. **Docker Status**: Verify Docker is running
2. **Container Status**: Check emulator container state
3. **Endpoint Connectivity**: Test HTTPS endpoint response
4. **Database Validation**: Verify database and containers exist
5. **System Information**: Display environment details

### test-all-cosmos.ps1

**Purpose**: Complete test suite orchestration

**Key Features**:

- Unit, integration, and service level test execution
- Comprehensive reporting and analytics
- Environment management
- Combined HTML reports

**Parameters**:

```powershell
./Scripts/test-all-cosmos.ps1 [options]

-SkipUnit         # Skip unit tests
-SkipIntegration  # Skip integration tests
-SkipService      # Skip service level tests
-HealthCheck      # Run health check first
-StopCosmosAfter  # Stop emulator when done
```

## Configuration

### appsettings.Development.json

The Cosmos DB connection is configured in your development settings:

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

### Environment Variables

Test scripts set these environment variables automatically:

```bash
ASPNETCORE_ENVIRONMENT=Development
USE_COSMOS_DB=true
COSMOS_DB_CONNECTION_STRING=<emulator_connection_string>
```

### Docker Configuration

The emulator runs with these settings:

```yaml
# docker-compose.cosmos.yml
services:
  cosmos-emulator:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    ports:
      - "8081:8081"      # Main endpoint
      - "10251:10251"    # Additional ports
      - "10252:10252"
      - "10253:10253"
      - "10254:10254"
    environment:
      - AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10
      - AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true
      - AZURE_COSMOS_EMULATOR_IP_ADDRESS_OVERRIDE=127.0.0.1
```

## Test Categories

### API Smoke Tests

- Basic endpoint availability
- Authentication flow validation
- Response format verification

### Integration Tests

- Full API workflow testing
- Data persistence validation
- Cross-service integration
- Error handling scenarios

### Chat API Tests

- Message sending/receiving
- Conversation management
- Real-time functionality
- Data consistency

### JWT Authentication Tests

- Token generation/validation
- Refresh token flow
- Authorization scenarios
- Security edge cases

### Google OAuth Tests

- OAuth flow integration
- Token exchange
- User profile creation
- Error handling

## Database Schema

The test scripts automatically create these containers:

### UserProfiles Container

- **Partition Key**: `/id`
- **Indexing**: Optimized for user queries
- **Purpose**: User profile data and preferences

### DateMarks Container

- **Partition Key**: `/userId`
- **Geospatial Indexing**: `/location/*`
- **Composite Indexes**: userId + visitDate, userId + createdAt
- **Purpose**: Location-based date marks with geospatial queries

### ChatMessages Container

- **Partition Key**: `/conversationId`
- **Purpose**: Chat message storage and retrieval

### Conversations Container

- **Partition Key**: `/id`
- **Purpose**: Conversation metadata and participants

## Troubleshooting

### Common Issues

#### Emulator Won't Start

```bash
# Check Docker status
docker ps

# View emulator logs
docker logs mapme-cosmos-emulator

# Restart emulator
./Scripts/stop-cosmos.ps1
./Scripts/start-cosmos.ps1
```

#### SSL Certificate Errors

The emulator uses self-signed certificates. Tests automatically disable SSL validation:

```csharp
var httpClientHandler = new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = 
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};
```

#### Port Conflicts

Default ports: 8081, 10251-10254. Check for conflicts:

```bash
# Check port usage (macOS)
lsof -i :8081

# Kill conflicting processes
sudo kill -9 <PID>
```

#### Memory Issues

Emulator requires significant memory:

```bash
# Check Docker memory allocation
docker system df

# Increase Docker memory in Docker Desktop settings
# Recommended: 4GB+ for emulator
```

### Test Failures

#### Database Not Found

```bash
# Reinitialize database
./Scripts/init-cosmosdb.ps1

# Or restart with initialization
./Scripts/start-cosmos.ps1
```

#### Connection Timeouts

```bash
# Verify emulator is responding
curl -k https://localhost:8081/_explorer/emulator.pem

# Check container health
docker inspect mapme-cosmos-emulator
```

#### Test Data Conflicts

```bash
# Clear test data
./Scripts/stop-cosmos.ps1
docker volume rm mapme_cosmos-data
./Scripts/start-cosmos.ps1
```

## Performance Considerations

### Test Execution Time

- **Unit Tests**: ~30 seconds
- **Integration Tests**: ~2-5 minutes
- **Service Level Tests**: ~5-10 minutes
- **Complete Suite**: ~10-15 minutes

### Optimization Tips

1. **Parallel Execution**: Tests run in parallel where possible
2. **Container Reuse**: Keep emulator running between test runs
3. **Selective Testing**: Use filters for specific test categories
4. **Resource Allocation**: Ensure adequate Docker memory

## Azure Storage Explorer Integration

### Connection Setup

1. Open Azure Storage Explorer
2. Right-click "Local & Attached" → "Connect to Azure Storage"
3. Select "Attach to a local emulator"
4. Enter connection details:
    - **Display Name**: "MapMe Local Cosmos DB"
    - **Endpoint**: `https://localhost:8081/`
    - **Account Key**: `C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==`

### Database Inspection

- Browse databases and containers
- Execute SQL queries
- Monitor performance metrics
- View document structure

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Service Level Tests

on: [push, pull_request]

jobs:
  service-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
        
    - name: Start Cosmos DB Emulator
      run: |
        docker run -d --name cosmos-emulator \
          -p 8081:8081 -p 10251:10251 -p 10252:10252 \
          mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
        
    - name: Wait for Emulator
      run: |
        timeout 300 bash -c 'until curl -k https://localhost:8081/_explorer/emulator.pem; do sleep 5; done'
        
    - name: Run Service Tests
      run: ./Scripts/test-service-cosmos.ps1 -NoHtml
      shell: pwsh
```

## Best Practices

### Development Workflow

1. **Start Emulator**: Begin development session
2. **Health Check**: Verify setup before testing
3. **Iterative Testing**: Run specific test categories during development
4. **Complete Suite**: Run full suite before commits
5. **Cleanup**: Stop emulator when done (optional)

### Test Data Management

- Tests use isolated data per execution
- Automatic cleanup between test runs
- Consistent test user IDs for reproducibility
- Geospatial test data for location features

### Security Considerations

- Emulator uses well-known development keys
- SSL validation disabled for local testing only
- Never use emulator keys in production
- Rotate production keys regularly

## Advanced Usage

### Custom Test Scenarios

```bash
# Run only chat-related tests
./Scripts/test-service-cosmos.ps1 -TestFilter "Chat"

# Run with custom output directory
./Scripts/test-service-cosmos.ps1 -OutputDir "CustomResults"

# Run integration tests for specific API
./Scripts/test-integration-cosmos.ps1 -TestCategory "JwtApi"
```

### Debugging Tests

```bash
# Run with maximum verbosity
./Scripts/test-all-cosmos.ps1 -Verbose -HealthCheck

# Check detailed health information
./Scripts/test-cosmos-health.ps1 -Detailed

# Monitor container logs during tests
docker logs -f mapme-cosmos-emulator
```

### Performance Testing

```bash
# Run tests multiple times for performance analysis
for i in {1..5}; do
  ./Scripts/test-service-cosmos.ps1 -NoHtml
done

# Monitor resource usage
docker stats mapme-cosmos-emulator
```

This comprehensive testing setup ensures MapMe's service level functionality works correctly with Cosmos DB, providing
confidence for production deployment.
