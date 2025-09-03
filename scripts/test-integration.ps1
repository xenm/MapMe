# MapMe Integration Tests Runner (PowerShell)
# Runs Integration tests with automatic repository detection
# Uses Cosmos DB if configured via environment variables, otherwise in-memory repositories
# Uses Category!=Unit filter to run all non-Unit tests

param(
    [switch]$NoHtml,
    [string]$OutputDir = "../TestResults/Integration"
)

# Check repository configuration
if ($env:CosmosDb__Endpoint -and $env:CosmosDb__Key)
{
    Write-Host "🔗 Running Integration tests with Cosmos DB repositories (endpoint: $( $env:CosmosDb__Endpoint ))" -ForegroundColor Green
}
else
{
    Write-Host "💾 Running Integration tests with in-memory repositories (no Cosmos DB configuration)" -ForegroundColor Green
}

# Create output directory with timestamp
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$fullOutputDir = "$OutputDir/$timestamp"
New-Item -ItemType Directory -Force -Path $fullOutputDir | Out-Null

# Set output file paths
$trxFile = "$fullOutputDir/Integration.trx"
$htmlFile = "$fullOutputDir/test-report.html"

try
{
    # Run Integration tests with filter (all tests except Unit)
    dotnet test --configuration Release --filter "Category!=Unit" --logger "trx;LogFileName=$trxFile" --verbosity normal

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Integration tests failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "Results (.trx): $trxFile" -ForegroundColor Cyan

    # Generate HTML report if trxlog2html is available and not disabled
    if (-not $NoHtml -and (Get-Command trxlog2html -ErrorAction SilentlyContinue))
    {
        Write-Host "Generating HTML test report via trxlog2html..." -ForegroundColor Yellow
        trxlog2html -i $trxFile -o $htmlFile
        if ($LASTEXITCODE -eq 0)
        {
            Write-Host "HTML test report: $htmlFile" -ForegroundColor Cyan
        }
        else
        {
            Write-Host "Failed to generate HTML report" -ForegroundColor Yellow
        }
    }
    elseif (-not $NoHtml)
    {
        Write-Host "trxlog2html not found - skipping HTML report generation" -ForegroundColor Yellow
        Write-Host "Install with: npm install -g trxlog2html" -ForegroundColor Yellow
    }

}
catch
{
    Write-Host "Error running Integration tests: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Integration tests completed successfully!" -ForegroundColor Green
