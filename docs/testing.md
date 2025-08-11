# Testing Strategy

## Test Types and Scopes

### Automated Tests
- **Unit Tests**: `Category!=Service` - Fast tests for individual components and services
- **Service Tests**: `Category=Service` - Integration tests using in-memory repositories
- **Manual Tests**: UI/UX testing for map functionality and user interactions

### Automated Test Infrastructure

**Service Tests (Integration)**
- Use `WebApplicationFactory<Program>` with in-memory repositories
- Test API endpoints end-to-end without external dependencies
- Run via: `./scripts/test-service.sh` or `pwsh scripts/test-service.ps1`
- Generate both TRX and HTML reports

**Unit Tests**
- Test individual components in isolation
- Run via: `dotnet test --filter "Category!=Service"`
- Fast execution, suitable for parallel runs

**Test Reporting**
- **TRX files**: Machine-readable results for CI/CD integration
- **HTML reports**: User-friendly reports generated via `trxlog2html`
- Reports located at: `TestResults/Service/<timestamp>/`

### Running Tests Locally

**Prerequisites**
- .NET 10 SDK

**Quick Commands**
```bash
# Run all service tests with HTML reports
./scripts/test-service.sh

# Run only unit tests
dotnet test --filter "Category!=Service"

# Run all tests
dotnet test
```

**HTML Report Setup** (one-time)
```bash
cd MapMe
dotnet tool install trxlog2html
```

### CI/CD Integration
- Azure Pipelines defined in `azure-pipelines.yml`
- Unit and service tests run in separate jobs
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

**Service Test Configuration**
- Tests use `ApiSmokeTests` class with `[Trait("Category", "Service")]`
- `WebApplicationFactory` configured with in-memory repositories:
  - `InMemoryUserProfileRepository`
  - `InMemoryDateMarkByUserRepository`
- No Cosmos DB Emulator required

**Test Isolation**
- Each test gets fresh repository instances
- No shared state between tests
- Fast execution (~1 second for service tests)

For detailed CI/CD and local testing instructions, see [ci-local-testing.md](ci-local-testing.md).
