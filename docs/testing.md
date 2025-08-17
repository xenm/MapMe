# Testing Strategy

## Test Types and Scopes

### Automated Tests
- **Unit Tests**: `Category=Unit` — Fast tests for individual components and services
- **Integration Tests**: `Category!=Unit` — API tests using in-memory repositories
- **Manual Tests**: UI/UX testing for map functionality and user interactions

### Automated Test Infrastructure

**Integration Tests**
- Use `WebApplicationFactory<Program>` with in-memory repositories
- Test API endpoints end-to-end without external dependencies
- Run via dotnet CLI (see commands below)
- Generate TRX (and optional HTML) reports

**Unit Tests**
- Test individual components in isolation
- Run via dotnet CLI filters
- Fast execution, suitable for parallel runs

**Test Reporting**
- **TRX files**: Machine-readable results for CI/CD integration
- **HTML reports**: User-friendly reports generated via `trxlog2html`
- Reports located at: `TestResults/<Type>/<timestamp>/`

### Running Tests Locally

**Prerequisites**
- .NET 10 SDK

**Quick Commands**
```bash
# Run Unit tests only (fast)
dotnet test MapMe/MapMe/MapMe.Tests --filter "Category=Unit"

# Run Integration tests (in-memory repositories)
dotnet test MapMe/MapMe/MapMe.Tests --filter "Category!=Unit"

# Run all tests
dotnet test MapMe/MapMe/MapMe.Tests
```

**HTML Report Setup** (one-time)
```bash
cd MapMe
dotnet tool install trxlog2html
```

To generate HTML from TRX manually:
```bash
trxlog2html TestResults/Integration/<timestamp>/test.trx -o TestResults/Integration/<timestamp>/test-report.html
```

### CI/CD Integration
- Azure Pipelines defined in `azure-pipelines.yml`
- Unit and integration tests run in separate jobs
- No external dependencies required (uses in-memory repositories)
- Test results published as pipeline artifacts

## Manual Test Checklist

### Map Functionality
- Map loads with API key and centers correctly
- Mock marks render via `MapMe.debugRenderMockMarks()`
- Grouping works: same placeId consolidates; proximity clusters within ~25m
- Marker overlay shows base place image, user chips, +N counter
- Hover label shows place title (or correct fallback)

### Interactive Features
- Clicking overlay opens info window with:
  - Place photos strip (scrollable)
  - Per-user sections with message list and user photos (scrollable)
  - Clicking images opens lightbox and navigates
- Hover/click on user name shows popover with profile details and recent photos
- Popover action buttons navigate correctly

### Performance Checks
- Avoid excessive re-renders; ensure listeners are cleaned up
- Check image loading and caching behavior
- Monitor memory usage during extended map interactions

## Test Architecture

**Integration Test Configuration**
- Tests use `Api*` classes with appropriate `[Trait]` attributes
- `WebApplicationFactory` configured with in-memory repositories:
  - `InMemoryUserProfileRepository`
  - `InMemoryDateMarkByUserRepository`
- No Cosmos DB Emulator required

**Test Isolation**
- Each test gets fresh repository instances
- No shared state between tests
- Fast execution (~1 second for service tests)

For detailed CI/CD and local testing instructions, see [ci-local-testing.md](ci-local-testing.md).
