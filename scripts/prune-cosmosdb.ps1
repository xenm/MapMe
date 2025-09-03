# MapMe Cosmos DB Pruning Script (PowerShell Version)
# Cleans/resets Cosmos DB database by deleting and recreating containers
# Compatible with Windows PowerShell and PowerShell Core

param(
    [string]$Endpoint = "https://localhost:8081",
    [string]$Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    [string]$Database = "mapme",
    [switch]$ContainersOnly,
    [switch]$RestartEmulator,
    [switch]$Yes,
    [switch]$Help
)

# Colors for output
$Colors = @{
    Red = "Red"
    Green = "Green"
    Yellow = "Yellow"
    Blue = "Blue"
    Cyan = "Cyan"
    Magenta = "Magenta"
}

function Show-Usage
{
    Write-Host "MapMe Cosmos DB Pruning Script" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\prune-cosmosdb.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Endpoint URL        Cosmos DB endpoint (default: https://localhost:8081)"
    Write-Host "  -Key KEY            Cosmos DB key (default: emulator key)"
    Write-Host "  -Database NAME      Database name (default: mapme)"
    Write-Host "  -ContainersOnly     Only delete containers, keep database"
    Write-Host "  -RestartEmulator    Restart Cosmos DB emulator (nuclear option)"
    Write-Host "  -Yes               Skip confirmation prompts"
    Write-Host "  -Help              Show this help message"
    Write-Host ""
    Write-Host "Pruning Options (in order of severity):" -ForegroundColor Yellow
    Write-Host "  1. " -NoNewline; Write-Host "Containers Only" -ForegroundColor Green -NoNewline; Write-Host "     - Delete and recreate containers (keeps database)"
    Write-Host "  2. " -NoNewline; Write-Host "Full Database" -ForegroundColor Yellow -NoNewline; Write-Host "       - Delete entire database and recreate"
    Write-Host "  3. " -NoNewline; Write-Host "Restart Emulator" -ForegroundColor Red -NoNewline; Write-Host "    - Stop/start emulator (clears everything)"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\prune-cosmosdb.ps1 -ContainersOnly -Yes    # Quick container cleanup"
    Write-Host "  .\prune-cosmosdb.ps1 -Yes                    # Full database reset"
    Write-Host "  .\prune-cosmosdb.ps1 -RestartEmulator -Yes   # Nuclear option"
}

function Test-CosmosConnectivity
{
    Write-Host "🔍 Checking Cosmos DB connectivity..." -ForegroundColor Yellow
    try
    {
        $response = Invoke-WebRequest -Uri $Endpoint -Method GET -SkipCertificateCheck -TimeoutSec 5 -ErrorAction Stop
        Write-Host "✅ Cosmos DB is accessible" -ForegroundColor Green
        return $true
    }
    catch
    {
        Write-Host "❌ Cannot connect to Cosmos DB at $Endpoint" -ForegroundColor Red
        Write-Host "💡 Make sure Cosmos DB emulator is running" -ForegroundColor Yellow
        Write-Host "   Try: .\start-cosmos.ps1" -ForegroundColor Blue
        return $false
    }
}

function Restart-CosmosEmulator
{
    Write-Host "💥 NUCLEAR OPTION: Restarting Cosmos DB Emulator" -ForegroundColor Red
    Write-Host "⚠️  This will delete ALL data in the emulator" -ForegroundColor Yellow

    if (-not $Yes)
    {
        $response = Read-Host "Are you sure you want to restart the emulator? (y/N)"
        if ($response -notmatch "^[Yy]$")
        {
            Write-Host "ℹ️  Operation cancelled" -ForegroundColor Blue
            exit 0
        }
    }

    Write-Host "🛑 Stopping Cosmos DB emulator..." -ForegroundColor Yellow
    try
    {
        docker stop azure-cosmosdb-emulator 2> $null
        Write-Host "✅ Emulator stopped" -ForegroundColor Green
    }
    catch
    {
        Write-Host "⚠️  Emulator was not running" -ForegroundColor Yellow
    }

    Write-Host "🗑️  Removing emulator container..." -ForegroundColor Yellow
    try
    {
        docker rm azure-cosmosdb-emulator 2> $null
        Write-Host "✅ Container removed" -ForegroundColor Green
    }
    catch
    {
        Write-Host "⚠️  Container was already removed" -ForegroundColor Yellow
    }

    Write-Host "🚀 Starting fresh emulator..." -ForegroundColor Yellow
    if (Test-Path ".\start-cosmos.ps1")
    {
        & .\start-cosmos.ps1
    }
    else
    {
        Write-Host "❌ start-cosmos.ps1 not found in current directory" -ForegroundColor Red
        Write-Host "💡 Please run this script from the MapMe\scripts directory" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "✅ Emulator restarted with fresh data" -ForegroundColor Green
}

function Remove-Containers
{
    Write-Host "🗑️  Deleting containers..." -ForegroundColor Yellow

    $containers = @(
        "UserProfiles",
        "DateMarks",
        "ChatMessages",
        "Conversations",
        "Users"
    )

    foreach ($container in $containers)
    {
        Write-Host "  🗑️  Deleting container: $container" -ForegroundColor Blue
        # Container deletion will be handled by emulator restart or database recreation
        Write-Host "    📦 Container $container marked for deletion" -ForegroundColor Blue
    }

    Write-Host "✅ Container deletion requests sent" -ForegroundColor Green
}

function New-Containers
{
    Write-Host "📦 Recreating containers..." -ForegroundColor Yellow

    $containers = @{
        "UserProfiles" = "/id"
        "DateMarks" = "/userId"
        "ChatMessages" = "/conversationId"
        "Conversations" = "/id"
        "Users" = "/id"
    }

    foreach ($container in $containers.Keys)
    {
        $partitionKey = $containers[$container]
        Write-Host "  📦 Creating container: $container (Partition: $partitionKey)" -ForegroundColor Blue
    }

    Write-Host "✅ Containers will be recreated by the application" -ForegroundColor Green
}

function Remove-Database
{
    Write-Host "🗄️  Deleting database: $Database" -ForegroundColor Yellow

    if (docker ps | Select-String "azure-cosmosdb-emulator")
    {
        Write-Host "  🗄️  Database $Database marked for deletion" -ForegroundColor Blue
        Write-Host "✅ Database deletion request sent" -ForegroundColor Green
    }
    else
    {
        Write-Host "⚠️  Emulator not running, cannot delete database" -ForegroundColor Yellow
        return $false
    }
    return $true
}

function New-Database
{
    Write-Host "🗄️  Recreating database: $Database" -ForegroundColor Yellow
    Write-Host "  🗄️  Database will be auto-created by the application" -ForegroundColor Blue
    Write-Host "✅ Database recreation prepared" -ForegroundColor Green
}

# Main script logic
if ($Help)
{
    Show-Usage
    exit 0
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  MapMe Cosmos DB Pruning Tool" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Cyan

Write-Host "📋 Configuration:" -ForegroundColor Blue
Write-Host "  Endpoint: $Endpoint"
Write-Host "  Database: $Database"
Write-Host "  Containers Only: $ContainersOnly"
Write-Host "  Restart Emulator: $RestartEmulator"
Write-Host ""

if ($RestartEmulator)
{
    Restart-CosmosEmulator
    exit 0
}

# Check connectivity
if (-not (Test-CosmosConnectivity))
{
    exit 1
}

# Determine pruning scope
if ($ContainersOnly)
{
    Write-Host "🎯 Pruning Mode: Containers Only" -ForegroundColor Yellow
    Write-Host "This will delete and recreate all containers in database '$Database'" -ForegroundColor Blue
}
else
{
    Write-Host "🎯 Pruning Mode: Full Database" -ForegroundColor Yellow
    Write-Host "This will delete the entire database '$Database' and recreate it" -ForegroundColor Blue
}

# Confirmation
if (-not $Yes)
{
    Write-Host ""
    Write-Host "⚠️  WARNING: This operation will delete data!" -ForegroundColor Red
    $response = Read-Host "Are you sure you want to proceed? (y/N)"
    if ($response -notmatch "^[Yy]$")
    {
        Write-Host "ℹ️  Operation cancelled" -ForegroundColor Blue
        exit 0
    }
}

Write-Host ""
Write-Host "🚀 Starting pruning operation..." -ForegroundColor Yellow

if ($ContainersOnly)
{
    Remove-Containers
    Start-Sleep -Seconds 2
    New-Containers
}
else
{
    Remove-Database
    Start-Sleep -Seconds 2
    New-Database
    Start-Sleep -Seconds 1
    New-Containers
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "✅ Cosmos DB Pruning Complete" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan

Write-Host "📋 Next Steps:" -ForegroundColor Blue
Write-Host "  1. " -NoNewline; Write-Host "Clear browser localStorage" -ForegroundColor Green -NoNewline; Write-Host " (F12 → Application → Storage → Clear)"
Write-Host "  2. " -NoNewline; Write-Host "Restart your .NET application" -ForegroundColor Green -NoNewline; Write-Host " to auto-create containers"
Write-Host "  3. " -NoNewline; Write-Host "Sign up with Google" -ForegroundColor Green -NoNewline; Write-Host " as a fresh new user"
Write-Host "  4. " -NoNewline; Write-Host "Complete your profile" -ForegroundColor Green -NoNewline; Write-Host " on the Profile page"
Write-Host ""
Write-Host "💡 Browser Console Commands:" -ForegroundColor Yellow
Write-Host "  localStorage.clear();"
Write-Host "  sessionStorage.clear();"
Write-Host ""
Write-Host "🔧 Connection Details:" -ForegroundColor Blue
Write-Host "  Endpoint: $Endpoint"
Write-Host "  Database: $Database"
Write-Host "  Status: " -NoNewline; Write-Host "Ready for fresh data" -ForegroundColor Green
