# CI and Local Testing Guide

This guide explains how the Azure Pipeline is set up, how to run the same steps locally, and how to work with the test infrastructure.

## Quick: run service tests via scripts (recommended)

From the MapMe folder:

- Bash (macOS/Linux)
  - `bash scripts/test-service.sh`
- PowerShell (Windows/macOS/Linux)
  - `pwsh scripts/test-service.ps1`

These scripts will:
- Run only `Category=Service` tests using in-memory repositories
- Produce a TRX test results file at `MapMe/TestResults/Service/<timestamp>/Service.trx`
- If the `reportgenerator` dotnet tool is installed, also generate `index.html` in the same folder

**No external dependencies required** - service tests use in-memory implementations for fast, reliable testing.

Optional environment variables for `scripts/test-service.sh`:
- `TEST_RESULTS_DIR` — override results output directory (defaults to `MapMe/TestResults/Service/<timestamp>`)

## Overview

- Azure Pipelines is defined in `azure-pipelines.yml`.
- .NET 10 SDK builds the solution.
- Tests are split:
  - Unit tests: `Category!=Service`
  - Service tests: `Category=Service` (uses in-memory repositories for integration testing)
- Merge is blocked when the PR validation pipeline fails (configure Branch Policies).

## Pipeline Variables

Configurable variables (set in the pipeline UI or variable groups):
- `buildConfiguration` (default: `Release`)
- `DOTNET_VERSION` (default: `10.0.x`)
- `TEST_RESULTS_DIR` (default: `TestResults`)
- `GOOGLE_MAPS_API_KEY` (optional)

## Running the same steps locally

Prerequisites:
- .NET 10 SDK

1) Restore and build

```bash
dotnet restore MapMe.sln
dotnet build MapMe.sln -c Release --no-restore
```

2) Run unit tests only (in parallel by default)

```bash
cd MapMe
dotnet test MapMe.Tests/MapMe.Tests.csproj -c Release --filter "Category!=Service"
```

3) Run service tests (integration tests with in-memory repositories)

```bash
cd MapMe
dotnet test MapMe.Tests/MapMe.Tests.csproj -c Release --filter "Category=Service"
```

Or use the convenience script:

```bash
cd MapMe
./scripts/test-service.sh
```

## Running the app + emulator with docker-compose

We provide `docker-compose.yml` to spin up both services.

```bash
docker compose up --build
# App available at http://localhost:8080
```

This is useful for end-to-end testing or manual exploration.

## Azure Repos PR merge blocking

To block merges when tests fail:
1. In Azure DevOps, navigate to Project Settings > Repositories > [your repo] > Branches.
2. Select your protected branch (e.g., `main`) > Branch policies.
3. Under Build validation, add a policy that triggers this pipeline on PRs.
4. Enable "Block merge if build fails" and require a successful run.

## Notes

- Tests are tagged with xUnit `Trait("Category", "Service")` for service tests. Add this trait to any test requiring integration testing with in-memory repositories.
- Unit tests run in parallel by default; use `[Collection]` or `xunit.runner.json` if you need custom parallelization.
- .NET 10 is used across the solution; no Newtonsoft.Json is used (System.Text.Json preferred).
- Service tests use `WebApplicationFactory<Program>` with in-memory repository implementations for fast, reliable integration testing.

## Troubleshooting: Service tests in CI

If the Service_Tests job fails:

- **Test discovery filters**
  - Only tests with `Trait("Category","Service")` run in Service_Tests. Verify your test has the correct trait.

- **Dependency injection issues**
  - Service tests configure `WebApplicationFactory` to use in-memory repositories. Ensure new repositories are properly registered in the test configuration.

- **Test isolation**
  - Each test gets a fresh instance of in-memory repositories. If tests are interfering with each other, check for shared state or timing issues.

## Real Cosmos DB Testing (Optional)

For testing against real Cosmos DB (not typically needed for CI):

1. Set up a Cosmos DB Emulator or Azure Cosmos DB instance
2. Configure environment variables:
   - `Cosmos__Endpoint` 
   - `Cosmos__Key`
   - `Cosmos__Database`
3. Run tests without the in-memory repository configuration

The service tests are designed to work with both in-memory and real Cosmos DB implementations through dependency injection.
