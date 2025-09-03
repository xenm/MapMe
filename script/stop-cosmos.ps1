#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stops CosmosDB emulator container
.DESCRIPTION
    This script stops and removes the CosmosDB emulator Docker container.
.PARAMETER KeepData
    Keep the container (don't remove) to preserve data
.EXAMPLE
    ./stop-cosmos.ps1
    ./stop-cosmos.ps1 -KeepData
#>

param(
    [switch]$KeepData
)

Write-Host "🛑 Stopping CosmosDB Emulator..." -ForegroundColor Yellow

# Stop the container
docker stop mapme-cosmos-emulator 2>$null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ CosmosDB emulator stopped" -ForegroundColor Green
} else {
    Write-Host "⚠️  CosmosDB emulator was not running" -ForegroundColor Yellow
}

# Remove container unless keeping data
if (-not $KeepData) {
    docker rm mapme-cosmos-emulator 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "🗑️  Container removed" -ForegroundColor Green
    }
} else {
    Write-Host "💾 Container preserved for data persistence" -ForegroundColor Cyan
}

Write-Host "✅ CosmosDB cleanup completed!" -ForegroundColor Green
