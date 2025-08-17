# CI and Local Testing Guide

This guide explains how the Azure Pipeline is set up, how to run the same steps locally, and how to work with the test infrastructure.

## Quick: run tests locally

From the repository root:

```bash
# Unit tests only (fast)
dotnet test MapMe/MapMe/MapMe.Tests --filter "Category=Unit"

# Integration tests using in-memory repositories
dotnet test MapMe/MapMe/MapMe.Tests --filter "Category!=Unit"

# All tests
dotnet test MapMe/MapMe/MapMe.Tests
```

**No external dependencies required** — integration tests use in-memory implementations for fast, reliable testing.

## Overview

- Azure Pipelines is defined in `azure-pipelines.yml`.
- .NET 10 SDK builds the solution.
- Tests are split:
  - Unit tests: `Category=Unit`
  - Integration tests: `Category!=Unit` (uses in-memory repositories)
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
dotnet test MapMe.Tests/MapMe.Tests.csproj -c Release --filter "Category=Unit"
```

3) Run service tests (integration tests with in-memory repositories)

```bash
cd MapMe
dotnet test MapMe.Tests/MapMe.Tests.csproj -c Release --filter "Category!=Unit"
```

## HTML Test Reports

You can generate user-friendly HTML test reports from TRX files:

1. **Install the HTML report generator** (one-time setup):
   ```bash
   cd MapMe
   dotnet tool install trxlog2html
   ```

2. **Run tests** and create HTML manually:
   ```bash
   dotnet test MapMe.Tests -l "trx;LogFileName=test.trx"
   trxlog2html TestResults/test.trx -o TestResults/test-report.html
   ```

3. **View the HTML report**:
   - Open `TestResults/test-report.html` (or your chosen path) in your browser
   - The report shows test results in a clean, readable format with proper styling

The HTML reports provide a much better viewing experience than raw TRX files, especially for reviewing test results and sharing with team members.

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
