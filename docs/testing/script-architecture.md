# MapMe Script Architecture Guide

## Overview

The MapMe project uses a **unified script architecture** that automatically detects repository configuration and adapts
accordingly. This eliminates the need for separate scripts for different repository types.

## Core Principle: Configuration-Based Repository Detection

The application automatically detects which repository implementation to use based on environment variables:

- **Cosmos DB**: When `CosmosDb__Endpoint` and `CosmosDb__Key` are set
- **In-Memory**: When Cosmos DB configuration is missing or invalid

This detection happens in `Program.cs` and eliminates the need for duplicate scripts.

## Script Categories

### 1. Cosmos DB Management Scripts (8 files)

These scripts manage the Cosmos DB emulator lifecycle:

| Script                        | Purpose                                  | Platforms             |
|-------------------------------|------------------------------------------|-----------------------|
| `start-cosmos.{ps1,sh}`       | Start ARM-compatible emulator with HTTPS | Windows, macOS, Linux |
| `stop-cosmos.{ps1,sh}`        | Stop emulator (preserves data)           | Windows, macOS, Linux |
| `init-cosmosdb.{ps1,sh}`      | Initialize database and containers       | Windows, macOS, Linux |
| `test-cosmos-health.{ps1,sh}` | Health check and diagnostics             | Windows, macOS, Linux |

**Key Features:**

- Uses ARM-compatible Docker image: `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`
- Enables HTTPS protocol for .NET SDK compatibility: `--protocol https`
- Fast health check (12 attempts × 5s = 1 minute max)
- Automatic database initialization

### 2. Unified Test Scripts (6 files)

These scripts automatically adapt to repository configuration:

| Script                      | Purpose                       | Repository Detection     |
|-----------------------------|-------------------------------|--------------------------|
| `test-unit.{ps1,sh}`        | Unit tests (always in-memory) | Always in-memory         |
| `test-integration.{ps1,sh}` | Integration tests             | Auto-detect via env vars |
| `test-all.{ps1,sh}`         | Complete test suite           | Auto-detect via env vars |

**Repository Detection Logic:**

```bash
# Shell example
if [ -n "${CosmosDb__Endpoint:-}" ] && [ -n "${CosmosDb__Key:-}" ]; then
    echo "🔗 Using Cosmos DB repositories"
else
    echo "💾 Using in-memory repositories"
fi
```

## Usage Patterns

### Pattern 1: In-Memory Testing (Default)

```bash
# No environment variables needed
./Scripts/test-integration.sh
./Scripts/test-all.sh
```

**Result**: Uses in-memory repositories, fast execution

### Pattern 2: Cosmos DB Testing

```bash
# Set environment variables
export CosmosDb__Endpoint="https://localhost:8081"
export CosmosDb__Key="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
export CosmosDb__DatabaseName="mapme"
export CosmosDb__EnableSSLValidation="false"

# Start emulator
./Scripts/start-cosmos.sh

# Run tests (automatically detects Cosmos DB)
./Scripts/test-integration.sh
./Scripts/test-all.sh
```

**Result**: Uses Cosmos DB repositories, full integration testing

### Pattern 3: PowerShell on Windows

```powershell
# Set environment variables
$env:CosmosDb__Endpoint = "https://localhost:8081"
$env:CosmosDb__Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

# Start emulator and run tests
.\Scripts\start-cosmos.ps1
.\Scripts\test-all.ps1
```

## Architecture Benefits

### 1. **Eliminates Duplication**

- ❌ **Before**: `test-integration.sh` + `test-integration-cosmos.sh` (duplicate logic)
- ✅ **After**: `test-integration.sh` (single script, auto-detects repository)

### 2. **Follows Single Responsibility Principle**

- **Cosmos Management**: Start, stop, health check emulator
- **Test Execution**: Run tests with automatic repository detection
- **No Mixed Concerns**: Scripts don't duplicate application logic

### 3. **Environment-Driven Configuration**

- Uses same detection logic as the application (`Program.cs`)
- Consistent behavior between scripts and runtime
- Easy to switch between repository types

### 4. **Cross-Platform Compatibility**

- Shell scripts for macOS/Linux native support
- PowerShell scripts for Windows + PowerShell Core
- Identical functionality across platforms

## File Structure

```
Scripts/
├── Cosmos DB Management
│   ├── start-cosmos.{ps1,sh}      # Start emulator with HTTPS
│   ├── stop-cosmos.{ps1,sh}       # Stop emulator
│   ├── init-cosmosdb.{ps1,sh}     # Initialize database
│   └── test-cosmos-health.{ps1,sh} # Health diagnostics
└── Unified Test Scripts
    ├── test-unit.{ps1,sh}         # Unit tests (in-memory only)
    ├── test-integration.{ps1,sh}  # Integration tests (auto-detect)
    └── test-all.{ps1,sh}          # All tests (auto-detect)
```

## Test Results Organization

All scripts output to unified directory structure:

```
TestResults/
├── Unit/YYYYMMDD-HHMMSS/
├── Integration/YYYYMMDD-HHMMSS/
└── All/YYYYMMDD-HHMMSS/
```

## Environment Variables Reference

| Variable                        | Purpose                   | Example                                 |
|---------------------------------|---------------------------|-----------------------------------------|
| `CosmosDb__Endpoint`            | Cosmos DB endpoint URL    | `https://localhost:8081`                |
| `CosmosDb__Key`                 | Cosmos DB primary key     | `C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM...` |
| `CosmosDb__DatabaseName`        | Database name             | `mapme`                                 |
| `CosmosDb__EnableSSLValidation` | SSL validation (emulator) | `false`                                 |
| `ASPNETCORE_ENVIRONMENT`        | ASP.NET environment       | `Development`                           |

## Troubleshooting

### Issue: Tests Use Wrong Repository Type

**Cause**: Environment variables not set correctly
**Solution**: Check environment variables match expected format

### Issue: Cosmos DB Connection Fails

**Cause**: Emulator not running or wrong configuration
**Solution**:

1. Run `./Scripts/test-cosmos-health.sh`
2. Check `docker logs mapme-cosmos-emulator`
3. Verify environment variables

### Issue: Slow Startup

**Cause**: ARM compatibility or health check issues
**Solution**:

- Ensure using ARM-compatible image with `--protocol https`
- Health check now limited to 12 attempts (1 minute max)

## Migration from Old Architecture

### Removed Scripts (No Longer Needed)

- ❌ `test-service-cosmos.{ps1,sh}` → Use `test-integration.{ps1,sh}` with Cosmos env vars
- ❌ `test-integration-cosmos.{ps1,sh}` → Use `test-integration.{ps1,sh}` with Cosmos env vars
- ❌ `test-all-cosmos.{ps1,sh}` → Use `test-all.{ps1,sh}` with Cosmos env vars

### Migration Commands

```bash
# Old way (removed)
./Scripts/test-integration-cosmos.sh

# New way (unified)
export CosmosDb__Endpoint="https://localhost:8081"
export CosmosDb__Key="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
./Scripts/test-integration.sh
```

This unified architecture follows the **Don't Repeat Yourself (DRY)** principle and aligns with the application's
existing configuration-based repository detection logic.
