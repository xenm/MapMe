Param(
  [string]$Configuration = "Release"
)

$ErrorActionPreference = 'Stop'

# Normalize to MapMe folder
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir '..' '..')
$MapMeDir = Join-Path $RepoRoot 'MapMe'
Set-Location $MapMeDir

# Results directory (timestamped) and reporting
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$DefaultResultsDir = Join-Path $MapMeDir "TestResults\Service\$Timestamp"
$TestResultsDir = if ($env:TEST_RESULTS_DIR) { $env:TEST_RESULTS_DIR } else { $DefaultResultsDir }
New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null

function Run-Tests {
  Write-Host 'Running Service tests (using in-memory repositories)...'
  
  dotnet test 'MapMe.Tests/MapMe.Tests.csproj' `
    -c $Configuration `
    --filter 'Category=Service' `
    --logger "trx;LogFileName=Service.trx" `
    --results-directory $TestResultsDir

  Write-Host "Results (.trx): $TestResultsDir\Service.trx"

  # Check if trxlog2html is available as a dotnet tool
  $toolList = dotnet tool list 2>$null
  if ($toolList -and ($toolList -match 'trxlog2html')) {
    Write-Host 'Generating HTML test report via trxlog2html...'
    try {
      dotnet trxlog2html `
        -i "$TestResultsDir\Service.trx" `
        -o "$TestResultsDir\test-report.html"
      Write-Host "HTML test report: $TestResultsDir\test-report.html"
    }
    catch {
      Write-Warning "Failed to generate HTML test report: $_"
    }
  }
  else {
    Write-Host 'NOTE: trxlog2html not found. Install it with:'
    Write-Host '  dotnet tool install trxlog2html'
    Write-Host 'Then re-run this script to get an HTML test report.'
  }
}

Run-Tests
