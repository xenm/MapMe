# MapMe All Tests Runner (PowerShell)
# Runs ALL tests (Unit + Integration) without filtering
# Provides comprehensive test coverage across the entire test suite

param(
    [switch]$NoHtml,
    [string]$OutputDir = "../TestResults/All"
)

Write-Host "Running ALL tests (Unit + Integration)..." -ForegroundColor Green

# Create output directory with timestamp
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$fullOutputDir = "$OutputDir/$timestamp"
New-Item -ItemType Directory -Force -Path $fullOutputDir | Out-Null

# Set output file paths
$trxFile = "$fullOutputDir/All.trx"
$htmlFile = "$fullOutputDir/test-report.html"

try
{
    # Run all tests without filtering
    dotnet test --configuration Release --logger "trx;LogFileName=$trxFile" --verbosity normal

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Some tests failed!" -ForegroundColor Red
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
    Write-Host "Error running tests: $_" -ForegroundColor Red
    exit 1
}

Write-Host "All tests completed successfully!" -ForegroundColor Green
