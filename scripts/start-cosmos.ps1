#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Starts CosmosDB emulator and initializes MapMe database
.DESCRIPTION
    This script starts the CosmosDB emulator using Docker, waits for it to be ready,
    and then initializes the database with required containers.
.PARAMETER SkipInit
    Skip database initialization if already done
.PARAMETER Detached
    Run Docker container in detached mode
.EXAMPLE
    ./start-cosmos.ps1
    ./start-cosmos.ps1 -SkipInit
#>

param(
    [switch]$SkipInit,
    [switch]$Detached = $true
)

Write-Host "🚀 Starting CosmosDB Emulator for MapMe..." -ForegroundColor Green

# Check if Docker is running
try {
    docker version | Out-Null
}
catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Stop existing container if running
Write-Host "🛑 Stopping existing CosmosDB container..." -ForegroundColor Yellow
docker stop mapme-cosmos-emulator 2>$null | Out-Null
docker rm mapme-cosmos-emulator 2>$null | Out-Null

# Start CosmosDB emulator
Write-Host "🐳 Starting CosmosDB emulator container..." -ForegroundColor Yellow

$dockerArgs = @(
    "run"
    "--name", "mapme-cosmos-emulator"
    "--publish", "8081:8081"
    "--publish", "1234:1234"
    "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview"
    "--protocol", "https"
)

if ($Detached) {
    $dockerArgs = @("-d") + $dockerArgs
}

& docker @dockerArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start CosmosDB emulator" -ForegroundColor Red
    exit 1
}

Write-Host "⏳ Waiting for CosmosDB emulator to be ready..." -ForegroundColor Yellow

# Wait for emulator to be ready
$maxAttempts = 30
$attempt = 0
$ready = $false

while ($attempt -lt $maxAttempts -and -not $ready) {
    $attempt++
    Write-Host "   Attempt $attempt/$maxAttempts..." -ForegroundColor Gray
    
    try {
        # Test connection to emulator
        $response = Invoke-WebRequest -Uri "https://localhost:8081/_explorer/emulator.pem" -SkipCertificateCheck -TimeoutSec 5 2>$null
        if ($response.StatusCode -eq 200) {
            $ready = $true
        }
    }
    catch {
        Start-Sleep -Seconds 10
    }
}

if (-not $ready) {
    Write-Host "❌ CosmosDB emulator failed to start within timeout" -ForegroundColor Red
    Write-Host "💡 Try running: docker logs mapme-cosmos-emulator" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ CosmosDB emulator is ready!" -ForegroundColor Green

# Initialize database if not skipped
if (-not $SkipInit) {
    Write-Host "🔧 Initializing MapMe database..." -ForegroundColor Yellow
    
    $initScript = Join-Path $PSScriptRoot "init-cosmosdb.ps1"
    if (Test-Path $initScript) {
        & $initScript
    } else {
        Write-Host "⚠️  Database initialization script not found at: $initScript" -ForegroundColor Yellow
        Write-Host "💡 Run manually: ./Scripts/init-cosmosdb.ps1" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "🎉 CosmosDB setup completed!" -ForegroundColor Green
Write-Host "📊 Connection Details:" -ForegroundColor Cyan
Write-Host "   • Endpoint: https://localhost:8081" -ForegroundColor White
Write-Host "   • Key: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==" -ForegroundColor White
Write-Host "   • Database: mapme" -ForegroundColor White
Write-Host ""
Write-Host "🔗 CosmosDB Storage Explorer:" -ForegroundColor Cyan
Write-Host "   • Open Storage Explorer" -ForegroundColor White
Write-Host "   • Add CosmosDB Account" -ForegroundColor White
Write-Host "   • Use connection details above" -ForegroundColor White
Write-Host ""
Write-Host "⚡ Ready to run MapMe!" -ForegroundColor Green
Write-Host "💡 Next steps:" -ForegroundColor Cyan
Write-Host "   1. Copy appsettings.Development.sample.json to appsettings.Development.json" -ForegroundColor White
Write-Host "   2. Run: dotnet run --project MapMe/MapMe" -ForegroundColor White
