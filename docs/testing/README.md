# Testing Documentation

This section contains comprehensive testing documentation for MapMe, including unit testing, integration testing, and end-to-end testing strategies.

## Quick Navigation

| Document | Purpose |
|----------|----------|
| [Unit Testing](./unit-testing.md) | Unit testing guidelines and best practices |
| [Integration Testing](./integration-testing.md) | API and service integration testing |
| [End-to-End Testing](./e2e-testing.md) | Full application testing strategies |
| [Performance Testing](./performance-testing.md) | Load and stress testing procedures |
| [Test Data Management](./test-data.md) | Test data creation and management |
| [Mocking Strategies](./mocking.md) | Mocking patterns and best practices |
| [CI Testing](./ci-testing.md) | Automated testing in CI/CD pipelines |

## Testing Overview

MapMe implements comprehensive testing at multiple levels:

### Current Test Status

- **Total Tests**: 300 tests
- **Pass Rate**: 100% (300/300 passing)
- **Test Categories**: Unit, Repository, API Smoke, Core API, Extended API, Error Handling, Chat API
- **Coverage**: Comprehensive backend and API coverage

### Testing Stack
- **Framework**: xUnit.net for .NET testing
- **Mocking**: Moq framework for dependency mocking
- **Test Host**: WebApplicationFactory for integration testing
- **Assertions**: FluentAssertions for readable test assertions
- **Test Data**: In-memory repositories and test fixtures

## Test Architecture

### Test Layers
1. **Unit Tests**: Isolated component testing with mocked dependencies
2. **Repository Tests**: Data access layer testing with in-memory implementations
3. **API Smoke Tests**: Basic API endpoint validation
4. **Core API Integration Tests**: Standard API workflows and scenarios
5. **Extended API Integration Tests**: Complex scenarios and edge cases
6. **Error Handling Tests**: Exception handling and boundary conditions
7. **Chat API Integration Tests**: Real-time messaging functionality

### Test Organization
```
MapMe.Tests/
├── Unit/                       # Unit tests
│   ├── Services/              # Service layer tests
│   ├── Controllers/           # Controller tests
│   └── Models/                # Model validation tests
├── Integration/               # Integration tests
│   ├── Api/                   # API endpoint tests
│   ├── Repository/            # Data access tests
│   └── Authentication/       # Auth flow tests
├── TestUtilities/             # Shared test helpers
│   ├── Fixtures/              # Test data fixtures
│   ├── Builders/              # Test object builders
│   └── Extensions/            # Test extensions
└── README.md                  # Test documentation
```

## Testing Principles

### Test Quality Standards
- **Arrange-Act-Assert**: Clear test structure
- **Single Responsibility**: One assertion per test
- **Descriptive Names**: Self-documenting test names
- **Independent Tests**: No test dependencies
- **Fast Execution**: Quick feedback loops

### Test Data Management
- **In-Memory Repositories**: Fast, isolated testing
- **Test Fixtures**: Reusable test data setup
- **Builder Pattern**: Flexible test object creation
- **Data Isolation**: Each test uses fresh data

### Authentication Testing
- **TestAuthenticationService**: Consistent test authentication
- **Bearer Token Pattern**: Standard authorization headers
- **User Context**: Proper user identification in tests

## Running Tests

### Command Line
```bash
# All tests
dotnet test MapMe/MapMe.Tests

# Unit tests only
dotnet test MapMe/MapMe.Tests --filter "Category=Unit"

# Integration tests only
dotnet test MapMe/MapMe.Tests --filter "Category!=Unit"

# Specific test category
dotnet test MapMe/MapMe.Tests --filter "Category=ApiIntegration"

# With coverage
dotnet test MapMe/MapMe.Tests --collect:"XPlat Code Coverage"
```

### IDE Integration
- **JetBrains Rider**: Built-in test runner with debugging
- **Visual Studio**: Test Explorer with live testing
- **VS Code**: C# extension test integration

## Test Categories

### Unit Tests (21 tests)
- **JwtService Tests**: Token generation and validation
- **GoogleAuthenticationService Tests**: OAuth integration
- **UserProfileService Tests**: Profile management logic
- **DateMark Business Logic**: Core business rules

### Integration Tests (279 tests)
- **API Smoke Tests**: Basic endpoint validation
- **Core API Tests**: Standard CRUD operations
- **Extended API Tests**: Complex scenarios and filtering
- **Error Handling Tests**: Exception scenarios
- **Chat API Tests**: Messaging functionality

## Best Practices

### Writing Tests
- Use descriptive test names that explain the scenario
- Follow AAA pattern (Arrange, Act, Assert)
- Keep tests focused and independent
- Use meaningful assertions with clear error messages

### Test Maintenance
- Update tests with code changes
- Remove obsolete tests promptly
- Refactor test code for maintainability
- Monitor test execution time and optimize slow tests

### Continuous Integration
- All tests must pass before merge
- Automated test execution on pull requests
- Test coverage reporting and monitoring
- Performance regression detection

## Related Documentation

- [Backend Testing](../backend/testing.md) - Backend-specific testing approaches
- [Frontend Testing](../frontend/testing.md) - Blazor component testing
- [API Testing](../api/testing.md) - API endpoint testing strategies
- [CI/CD Testing](../deployment/ci-cd.md) - Automated testing pipelines

---

**Last Updated**: 2025-08-30  
**Test Coverage**: 100% (300/300 tests passing)  
**Maintained By**: Development Team
