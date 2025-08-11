# MapMe Unit Tests Runner (PowerShell)
# Runs only Unit tests (client-side logic and business rules)
# Uses Category=Unit filter to run tests tagged with [Trait("Category", "Unit")]

param(
    [switch]$NoHtml,
    [string]$OutputDir = "TestResults/Unit"
)

Write-Host "Running Unit tests (client-side logic and business rules)..." -ForegroundColor Green

# Create output directory with timestamp
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$fullOutputDir = "$OutputDir/$timestamp"
New-Item -ItemType Directory -Force -Path $fullOutputDir | Out-Null

# Set output file paths
$trxFile = "$fullOutputDir/Unit.trx"
$htmlFile = "$fullOutputDir/test-report.html"

try {
    # Run Unit tests with filter
    dotnet test --configuration Release --filter "Category=Unit" --logger "trx;LogFileName=$trxFile" --verbosity normal
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Unit tests failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    Write-Host "Results (.trx): $trxFile" -ForegroundColor Cyan
    
    # Generate HTML report if trxlog2html is available and not disabled
    if (-not $NoHtml -and (Get-Command trxlog2html -ErrorAction SilentlyContinue)) {
        Write-Host "Generating HTML test report via trxlog2html..." -ForegroundColor Yellow
        trxlog2html -i $trxFile -o $htmlFile
        if ($LASTEXITCODE -eq 0) {
            Write-Host "HTML test report: $htmlFile" -ForegroundColor Cyan
        } else {
            Write-Host "Failed to generate HTML report" -ForegroundColor Yellow
        }
    } elseif (-not $NoHtml) {
        Write-Host "trxlog2html not found - skipping HTML report generation" -ForegroundColor Yellow
        Write-Host "Install with: npm install -g trxlog2html" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Error running Unit tests: $_" -ForegroundColor Red
    exit 1
}

Write-Host "Unit tests completed successfully!" -ForegroundColor Green
