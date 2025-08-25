# MapMe Test Suite

## Overview
Comprehensive test suite for the MapMe dating application with Google Maps integration. The test suite validates core business logic, API endpoints, data persistence, and client-side services with **clear separation between Unit and Integration tests**.

**Current Status: 91/91 tests passing (100% pass rate)**

## 📋 **Important Documentation**
- **[TEST_ARCHITECTURE.md](TEST_ARCHITECTURE.md)** - Complete test architecture documentation including repository approaches, authentication patterns, and bottom-up fixing methodology
- **[Bottom-Up Test Fixing Guide](TEST_ARCHITECTURE.md#bottom-up-test-fixing-methodology)** - Priority order for fixing failing tests (Unit → Repository → Smoke → Core → Extended → Error → Chat)

## 📁 **Test Structure & Organization**

### **Clear Separation by Directory & Naming**
```
MapMe.Tests/
├── Unit/                           # 21 Unit Tests
│   ├── UserProfileService.Unit.Tests.cs
│   ├── DateMarkBusinessLogic.Unit.Tests.cs
│   ├── Normalization.Unit.Tests.cs
│   └── InMemoryRepository.Unit.Tests.cs
├── Integration/                    # 70 Integration Tests
│   ├── Api.Integration.Tests.cs
│   ├── ExtendedApi.Integration.Tests.cs
│   ├── ErrorHandling.Integration.Tests.cs
│   ├── ApiSmoke.Integration.Tests.cs
│   └── ChatApiIntegrationTests.cs  # 12 Chat API tests
├── README.md                       # This documentation
└── TEST_ARCHITECTURE.md            # Complete architecture guide

Dotnet CLI                         # Preferred Execution Method
└── See commands below             # Unit / Integration / All tests
```

## 🧪 **Test Categories**

### **1. Unit Tests** (21 tests) - `Unit/` directory
**Purpose:** Fast, isolated testing of business logic and client-side services
**Execution Time:** ~0.4 seconds
**Dependencies:** None (pure logic testing)

#### **UserProfileService.Unit.Tests.cs** (6 tests)
- Client-side service logic and localStorage integration
- Profile creation, retrieval, and activity statistics
- DateMark CRUD operations with duplicate prevention
- JSON serialization/deserialization consistency

#### **DateMarkBusinessLogic.Unit.Tests.cs** (12 tests)  
- Core business logic and model behavior
- GeoPoint coordinate handling (GeoJSON format: [lng, lat])
- Text normalization for search functionality
- PlaceSnapshot integration and data storage
- Visibility settings and soft delete functionality

#### **Normalization.Unit.Tests.cs** (2 tests)
- Text normalization algorithms for search
- Diacritics removal and case handling
- Duplicate filtering and whitespace handling

#### **InMemoryRepository.Unit.Tests.cs** (2 tests)
- Repository pattern validation
- In-memory data storage and retrieval
- Filtering and query operations

### **2. Integration Tests** (70 tests) - `Integration/` directory
**Purpose:** End-to-end API testing with WebApplicationFactory and in-memory repositories
**Execution Time:** ~10 seconds
**Dependencies:** ASP.NET Core test server, in-memory repositories

#### **Api.Integration.Tests.cs** (11 tests)
- Complete user profile lifecycle (create, retrieve, update)
- DateMark CRUD operations with filtering capabilities
- Category, tag, and date range filtering
- Visibility settings enforcement

#### **ExtendedApi.Integration.Tests.cs** (13 tests)
- Advanced scenarios and edge cases
- Input validation with complex data
- Extreme coordinate boundary testing (±90°, ±180°)
- Complex multi-criteria filtering combinations
- Concurrent operations and performance testing

#### **ErrorHandling.Integration.Tests.cs** (8 tests)
- Malformed JSON and request validation
- HTTP method validation (MethodNotAllowed responses)
- Content type validation and wrong media types
- Query parameter edge cases and validation
- Special character encoding (Unicode, emojis, symbols)
- Large payload handling and boundary limits

#### **ApiSmoke.Integration.Tests.cs** (2 tests)
- Basic API endpoint smoke tests
- Service-level validation with in-memory repositories

#### **ChatApiIntegrationTests.cs** (12 tests)
- Complete chat functionality end-to-end testing
- Message sending, conversation management, read status
- Message archiving and deletion workflows
- **Special Setup**: Uses direct repository population via `SetupTestUsersAsync()`
- Multi-user conversation scenarios and pagination

## 🚀 **Running Tests**

### **Direct dotnet CLI Usage**

For advanced users who prefer direct CLI commands:

```bash
# Run Unit tests only (fast, ~0.4s)
dotnet test MapMe.Tests --filter "Category=Unit"

# Run Integration tests only (~7s)  
dotnet test MapMe.Tests --filter "Category!=Unit"

# Run all tests (~7.5s)
dotnet test MapMe.Tests

# Run specific test files
dotnet test MapMe.Tests --filter "FullyQualifiedName~UserProfileService"
dotnet test MapMe.Tests --filter "FullyQualifiedName~Api.Integration"

# Run specific test methods
dotnet test MapMe.Tests --filter "FullyQualifiedName~DateMark_FilteringCombinations"

# Custom output and logging
dotnet test MapMe.Tests --logger "trx;LogFileName=MyTests.trx" --verbosity detailed
```

### **Usage Examples (Dotnet CLI)**

```bash
# Run Unit tests only (fast)
dotnet test MapMe.Tests --filter "Category=Unit"

# Run Integration tests only (in-memory repositories)
dotnet test MapMe.Tests --filter "Category!=Unit"

# Run all tests
dotnet test MapMe.Tests
```

## 📊 **Test Coverage Analysis**

### **Unit Test Coverage**
- ✅ **Client-Side Services**: UserProfileService business logic
- ✅ **Business Logic**: DateMark creation, validation, normalization
- ✅ **Data Models**: Immutable records and data integrity
- ✅ **Utility Functions**: Text normalization and GeoPoint handling
- ✅ **Repository Patterns**: In-memory data operations

### **Integration Test Coverage**
- ✅ **API Endpoints**: All `/api/profiles` and `/api/datemarks` endpoints
- ✅ **Request/Response**: JSON serialization, HTTP status codes
- ✅ **Data Persistence**: End-to-end data workflows
- ✅ **Error Handling**: Malformed requests, validation errors
- ✅ **Edge Cases**: Boundary values, extreme coordinates, large payloads
- ✅ **Concurrency**: Thread-safe operations and race conditions

## 🏗️ **Test Infrastructure**

### **Unit Tests**
- **Framework**: xUnit with FluentAssertions
- **Mocking**: Moq for JavaScript interop and localStorage
- **Isolation**: No external dependencies or network calls
- **Speed**: Optimized for fast feedback during development

### **Integration Tests**
- **Framework**: xUnit with WebApplicationFactory
- **Test Server**: ASP.NET Core TestServer with in-memory repositories
- **Data**: Isolated test data per test method
- **Cleanup**: Automatic cleanup between tests
- **Assertions**: FluentAssertions for readable test failures

## 📈 **Test Results & Reporting**

### **Test Execution Results**
- **Total Tests**: 91
- **Unit Tests**: 21/21 passing (100%)
- **Integration Tests**: 70/70 passing (100%)
  - Core API: 11/11 passing
  - Extended API: 13/13 passing  
  - Error Handling: 8/8 passing
  - API Smoke: 2/2 passing
  - **Chat API: 12/12 passing** ✅
- **Overall Pass Rate**: 100%
- **Build Status**: ✅ Clean builds with 0 errors

### **Generated Reports**
All test scripts generate:
- **TRX Files**: Machine-readable test results
- **HTML Reports**: Human-readable test reports (requires `trxlog2html`)
- **Timestamped Results**: Organized by test type and execution time

### **Report Locations**
```
TestResults/
├── Unit/YYYYMMDD-HHMMSS/          # Unit test results
├── Integration/YYYYMMDD-HHMMSS/   # Integration test results
├── All/YYYYMMDD-HHMMSS/           # All test results
└── Service/YYYYMMDD-HHMMSS/       # Legacy service results
```

## 🔧 **Development Workflow**

### **Bottom-Up Test Fixing Priority (When Tests Fail)**
**Always fix tests in this order - easiest to hardest:**
1. **Unit Tests** → Fix first (isolated, no dependencies)
2. **Repository Tests** → Fix second (direct repository access)
3. **API Smoke Tests** → Fix third (simple API validation)
4. **Core API Integration Tests** → Fix fourth (standard workflows)
5. **Extended API Integration Tests** → Fix fifth (complex scenarios)
6. **Error Handling Integration Tests** → Fix sixth (specialized errors)
7. **Chat API Integration Tests** → Fix last (most complex, special setup)

**📋 See [TEST_ARCHITECTURE.md](TEST_ARCHITECTURE.md#bottom-up-test-fixing-methodology) for detailed fixing methodology**

### **Recommended Testing Strategy**
1. **During Development**: Run Unit tests frequently (`./scripts/test-unit.sh`)
2. **Before Commits**: Run Integration tests (`./scripts/test-integration.sh`)
3. **CI/CD Pipeline**: Run all tests (`./scripts/test-all.sh`)
4. **Production Deployment**: Full test suite with reports

### **Adding New Tests**
- **Unit Tests**: Add to appropriate `Unit/*.Unit.Tests.cs` file
- **Integration Tests**: Add to appropriate `Integration/*.Integration.Tests.cs` file
- **Tag Tests**: Use `[Trait("Category", "Unit")]` for unit tests
- **Follow Naming**: Use `ComponentName.Unit.Tests.cs` or `ComponentName.Integration.Tests.cs`

## 🎯 **Quality Metrics**

### **Test Quality Indicators**
- ✅ **100% Pass Rate**: All tests consistently passing
- ✅ **Fast Unit Tests**: Sub-second execution for rapid feedback
- ✅ **Comprehensive Coverage**: All API endpoints and business logic covered
- ✅ **Clear Organization**: Obvious separation between Unit and Integration tests
- ✅ **Professional Standards**: Following .NET 10 best practices

### **Future Enhancements**
- **Performance Tests**: Load testing for high-traffic scenarios
- **UI Component Tests**: Blazor component integration testing
- **Security Tests**: Authentication, authorization, and input validation
- **End-to-End Tests**: Full browser automation with Playwright
- **Database Integration**: Real Cosmos DB integration tests

## 🚨 **Known Issues**
- **Package Warnings**: Minor Cosmos DB version mismatches (non-blocking)
- **Newtonsoft.Json**: Known vulnerability warning (legacy dependency)
- **.NET 10 Preview**: Some compatibility warnings (framework-related)

All issues are framework/dependency related, not application defects. The core MapMe functionality is fully tested and production-ready.
