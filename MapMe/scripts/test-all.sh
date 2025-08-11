#!/usr/bin/env bash
set -euo pipefail

# Run from the repo root or MapMe folder; normalize to MapMe folder for test path stability
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
MAPME_DIR="$REPO_ROOT/MapMe"

cd "$MAPME_DIR"

# Results directory (timestamped) and reporting
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
DEFAULT_RESULTS_DIR="$MAPME_DIR/TestResults/All/$TIMESTAMP"
TEST_RESULTS_DIR=${TEST_RESULTS_DIR:-$DEFAULT_RESULTS_DIR}
mkdir -p "$TEST_RESULTS_DIR"

run_tests() {
  echo "Running ALL tests (Unit + Integration)..."
  dotnet test MapMe.Tests/MapMe.Tests.csproj \
    -c Release \
    --logger "trx;LogFileName=All.trx" \
    --results-directory "$TEST_RESULTS_DIR"

  echo "Results (.trx): $TEST_RESULTS_DIR/All.trx"

  # Check if trxlog2html is available as a dotnet tool
  if dotnet tool list | grep -q trxlog2html; then
    echo "Generating HTML test report via trxlog2html..."
    dotnet trxlog2html \
      -i "$TEST_RESULTS_DIR/All.trx" \
      -o "$TEST_RESULTS_DIR/test-report.html" || true
    echo "HTML test report: $TEST_RESULTS_DIR/test-report.html"
  else
    echo "NOTE: 'trxlog2html' not found. Install it with:"
    echo "  dotnet tool install trxlog2html"
    echo "Then re-run this script to get an HTML test report."
  fi
}

run_tests
