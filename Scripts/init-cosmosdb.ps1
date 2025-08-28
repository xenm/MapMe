#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Initializes CosmosDB database and containers for MapMe application
.DESCRIPTION
    This script creates the MapMe database and required containers with proper indexing policies
    for optimal performance with user profiles and DateMarks.
.PARAMETER Endpoint
    CosmosDB endpoint URL (default: https://localhost:8081 for emulator)
.PARAMETER Key
    CosmosDB master key (default: emulator key)
.PARAMETER DatabaseName
    Database name to create (default: mapme)
.EXAMPLE
    ./init-cosmosdb.ps1
    ./init-cosmosdb.ps1 -Endpoint "https://your-cosmos.documents.azure.com:443/" -Key "your-key"
#>

param(
    [string]$Endpoint = "https://localhost:8081",
    [string]$Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    [string]$DatabaseName = "mapme"
)

Write-Host "🚀 Initializing CosmosDB for MapMe..." -ForegroundColor Green
Write-Host "📍 Endpoint: $Endpoint" -ForegroundColor Cyan
Write-Host "🗄️  Database: $DatabaseName" -ForegroundColor Cyan

# Check if Azure.Cosmos PowerShell module is available
if (-not (Get-Module -ListAvailable -Name "CosmosDB")) {
    Write-Host "⚠️  CosmosDB PowerShell module not found. Installing..." -ForegroundColor Yellow
    try {
        Install-Module -Name CosmosDB -Force -Scope CurrentUser
        Write-Host "✅ CosmosDB module installed successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to install CosmosDB module. Using REST API instead..." -ForegroundColor Red
    }
}

# Function to make REST API calls to CosmosDB
function Invoke-CosmosDbRequest {
    param(
        [string]$Method,
        [string]$ResourceType,
        [string]$ResourceId,
        [string]$Body = "",
        [hashtable]$Headers = @{}
    )
    
    $date = [DateTime]::UtcNow.ToString("r")
    $authString = [System.Web.HttpUtility]::UrlEncode("type=master&ver=1.0&sig=" + 
        [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(
            [System.Security.Cryptography.HMACSHA256]::new([Convert]::FromBase64String($Key)).ComputeHash(
                [System.Text.Encoding]::UTF8.GetBytes("$($Method.ToLower())`n$($ResourceType.ToLower())`n$ResourceId`n$($date.ToLower())`n`n")
            ) | ForEach-Object { $_.ToString("x2") }
        )))
    
    $requestHeaders = @{
        "Authorization" = $authString
        "x-ms-date" = $date
        "x-ms-version" = "2020-07-15"
        "Content-Type" = "application/json"
    }
    
    foreach ($key in $Headers.Keys) {
        $requestHeaders[$key] = $Headers[$key]
    }
    
    $uri = "$Endpoint/$ResourceType/$ResourceId"
    
    try {
        if ($Body) {
            return Invoke-RestMethod -Uri $uri -Method $Method -Headers $requestHeaders -Body $Body
        } else {
            return Invoke-RestMethod -Uri $uri -Method $Method -Headers $requestHeaders
        }
    }
    catch {
        Write-Host "❌ REST API call failed: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Create database
Write-Host "📦 Creating database '$DatabaseName'..." -ForegroundColor Yellow

$databaseBody = @{
    id = $DatabaseName
} | ConvertTo-Json

$database = Invoke-CosmosDbRequest -Method "POST" -ResourceType "dbs" -ResourceId "" -Body $databaseBody

if ($database) {
    Write-Host "✅ Database '$DatabaseName' created successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  Database might already exist or creation failed" -ForegroundColor Yellow
}

# Create UserProfiles container
Write-Host "📋 Creating UserProfiles container..." -ForegroundColor Yellow

$userProfilesContainer = @{
    id = "UserProfiles"
    partitionKey = @{
        paths = @("/id")
        kind = "Hash"
    }
    indexingPolicy = @{
        indexingMode = "consistent"
        automatic = $true
        includedPaths = @(
            @{ path = "/*" }
        )
        excludedPaths = @(
            @{ path = "/photos/*/data/*" }
        )
        compositeIndexes = @(
            @(
                @{ path = "/userId"; order = "ascending" },
                @{ path = "/updatedAt"; order = "descending" }
            )
        )
    }
} | ConvertTo-Json -Depth 10

$userProfilesResult = Invoke-CosmosDbRequest -Method "POST" -ResourceType "dbs/$DatabaseName/colls" -ResourceId "" -Body $userProfilesContainer

if ($userProfilesResult) {
    Write-Host "✅ UserProfiles container created successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  UserProfiles container might already exist or creation failed" -ForegroundColor Yellow
}

# Create DateMarks container with geospatial indexing
Write-Host "🗺️  Creating DateMarks container with geospatial indexing..." -ForegroundColor Yellow

$dateMarksContainer = @{
    id = "DateMarks"
    partitionKey = @{
        paths = @("/userId")
        kind = "Hash"
    }
    indexingPolicy = @{
        indexingMode = "consistent"
        automatic = $true
        includedPaths = @(
            @{ path = "/*" }
        )
        excludedPaths = @(
            @{ path = "/photos/*/data/*" }
        )
        spatialIndexes = @(
            @{
                path = "/location/*"
                types = @("Point")
            }
        )
        compositeIndexes = @(
            @(
                @{ path = "/userId"; order = "ascending" },
                @{ path = "/visitDate"; order = "descending" }
            ),
            @(
                @{ path = "/userId"; order = "ascending" },
                @{ path = "/createdAt"; order = "descending" }
            )
        )
    }
} | ConvertTo-Json -Depth 10

$dateMarksResult = Invoke-CosmosDbRequest -Method "POST" -ResourceType "dbs/$DatabaseName/colls" -ResourceId "" -Body $dateMarksContainer

if ($dateMarksResult) {
    Write-Host "✅ DateMarks container created successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  DateMarks container might already exist or creation failed" -ForegroundColor Yellow
}

# Create ChatMessages container (for future use)
Write-Host "💬 Creating ChatMessages container..." -ForegroundColor Yellow

$chatMessagesContainer = @{
    id = "ChatMessages"
    partitionKey = @{
        paths = @("/conversationId")
        kind = "Hash"
    }
    indexingPolicy = @{
        indexingMode = "consistent"
        automatic = $true
        compositeIndexes = @(
            @(
                @{ path = "/conversationId"; order = "ascending" },
                @{ path = "/createdAt"; order = "descending" }
            )
        )
    }
} | ConvertTo-Json -Depth 10

$chatMessagesResult = Invoke-CosmosDbRequest -Method "POST" -ResourceType "dbs/$DatabaseName/colls" -ResourceId "" -Body $chatMessagesContainer

if ($chatMessagesResult) {
    Write-Host "✅ ChatMessages container created successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  ChatMessages container might already exist or creation failed" -ForegroundColor Yellow
}

# Create Conversations container (for future use)
Write-Host "🗨️  Creating Conversations container..." -ForegroundColor Yellow

$conversationsContainer = @{
    id = "Conversations"
    partitionKey = @{
        paths = @("/id")
        kind = "Hash"
    }
    indexingPolicy = @{
        indexingMode = "consistent"
        automatic = $true
        compositeIndexes = @(
            @(
                @{ path = "/participant1Id"; order = "ascending" },
                @{ path = "/updatedAt"; order = "descending" }
            ),
            @(
                @{ path = "/participant2Id"; order = "ascending" },
                @{ path = "/updatedAt"; order = "descending" }
            )
        )
    }
} | ConvertTo-Json -Depth 10

$conversationsResult = Invoke-CosmosDbRequest -Method "POST" -ResourceType "dbs/$DatabaseName/colls" -ResourceId "" -Body $conversationsContainer

if ($conversationsResult) {
    Write-Host "✅ Conversations container created successfully" -ForegroundColor Green
} else {
    Write-Host "⚠️  Conversations container might already exist or creation failed" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 CosmosDB initialization completed!" -ForegroundColor Green
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "   • Database: $DatabaseName" -ForegroundColor White
Write-Host "   • UserProfiles container with user indexing" -ForegroundColor White
Write-Host "   • DateMarks container with geospatial indexing" -ForegroundColor White
Write-Host "   • ChatMessages container for messaging" -ForegroundColor White
Write-Host "   • Conversations container for chat management" -ForegroundColor White
Write-Host ""
Write-Host "🔗 Connect to CosmosDB Storage Explorer:" -ForegroundColor Cyan
Write-Host "   • Endpoint: $Endpoint" -ForegroundColor White
Write-Host "   • Key: $Key" -ForegroundColor White
Write-Host ""
Write-Host "⚡ Ready to run MapMe with CosmosDB!" -ForegroundColor Green
