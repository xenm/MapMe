# MapMe Test Architecture Documentation

## Overview
This document outlines the test architecture for the MapMe .NET 10 project, detailing which test layers use which approaches and repositories, and provides guidelines for test development and maintenance.

## Test Layer Architecture

### 1. Unit Tests
**Location**: `MapMe.Tests/Unit/`
**Purpose**: Test individual components in isolation
**Repository Approach**: Mock repositories using Moq framework
**Authentication**: Mock authentication services
**Data**: Fake/mock data objects
**Scope**: Single class/method testing

```csharp
// Example: Unit test with mocked repository
var mockRepo = new Mock<IUserProfileRepository>();
mockRepo.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeProfile);
```

### 2. Repository Tests
**Location**: `MapMe.Tests/Repository/`
**Purpose**: Test repository implementations directly
**Repository Approach**: In-memory repository implementations
**Authentication**: Not applicable (direct repository testing)
**Data**: Test data objects created in test methods
**Scope**: Repository CRUD operations

```csharp
// Example: Repository test with in-memory implementation
var repo = new InMemoryUserProfileRepository();
await repo.UpsertAsync(testProfile);
var result = await repo.GetAsync(testProfile.Id);
```

### 3. Integration Tests - API Smoke Tests
**Location**: `MapMe.Tests/Integration/ApiSmoke.Integration.Tests.cs`
**Purpose**: Basic API endpoint validation
**Repository Approach**: **Singleton** in-memory repositories via WebApplicationFactory
**Authentication**: TestAuthenticationService with Bearer tokens
**Data**: Simple test data created per test
**Scope**: Basic CRUD operations validation

```csharp
// WebApplicationFactory configuration for API Smoke Tests
services.RemoveAll<IUserProfileRepository>();
services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
```

### 4. Integration Tests - Core API
**Location**: `MapMe.Tests/Integration/Api.Integration.Tests.cs`
**Purpose**: Comprehensive API workflow testing
**Repository Approach**: **Singleton** in-memory repositories via WebApplicationFactory
**Authentication**: TestAuthenticationService with Bearer tokens ("test-session-token")
**Data**: Consistent test data using standardized userIds ("test_user_id")
**Scope**: Complete API workflows and business logic

### 5. Integration Tests - Extended API
**Location**: `MapMe.Tests/Integration/ExtendedApi.Integration.Tests.cs`
**Purpose**: Advanced scenarios and edge cases
**Repository Approach**: **Singleton** in-memory repositories via WebApplicationFactory
**Authentication**: TestAuthenticationService with Bearer tokens
**Data**: Complex test scenarios with boundary conditions
**Scope**: Advanced workflows, filtering, edge cases

### 6. Integration Tests - Error Handling
**Location**: `MapMe.Tests/Integration/ErrorHandling.Integration.Tests.cs`
**Purpose**: Error conditions and boundary testing
**Repository Approach**: **Singleton** in-memory repositories via WebApplicationFactory
**Authentication**: TestAuthenticationService with Bearer tokens
**Data**: Invalid/boundary test data designed to trigger errors
**Scope**: Error scenarios, malformed requests, boundary conditions

### 7. Integration Tests - Chat API
**Location**: `MapMe.Tests/Integration/ChatApiIntegrationTests.cs`
**Purpose**: Chat functionality end-to-end testing
**Repository Approach**: **Singleton** in-memory repositories via WebApplicationFactory
**Authentication**: TestAuthenticationService with Bearer tokens
**Data**: **Direct repository population** via `SetupTestUsersAsync()` method
**Scope**: Chat messages, conversations, real-time communication

```csharp
// Chat API specific approach - Direct repository population
private async Task SetupTestUsersAsync()
{
    using var scope = _factory.Services.CreateScope();
    var userRepo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
    
    var testUsers = new[]
    {
        new UserProfile(
            Id: "profile_test_user_id",
            UserId: "test_user_id",
            DisplayName: "Test User",
            Bio: "Test bio",
            Photos: Array.Empty<UserPhoto>(),
            Preferences: new UserPreferences(Array.Empty<string>()),
            Visibility: "public",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow
        )
    };

    foreach (var user in testUsers)
    {
        await userRepo.UpsertAsync(user);
    }
}
```

## Repository Configuration by Test Type

### Singleton vs Scoped Repository Registration

**Singleton Repositories (Integration Tests)**:
```csharp
// Used in all Integration Tests for data persistence across HTTP requests
services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
services.AddSingleton<IDateMarkByUserRepository, InMemoryDateMarkByUserRepository>();
services.AddSingleton<IChatMessageRepository, InMemoryChatMessageRepository>();
services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
```

**Scoped Repositories (Unit Tests)**:
```csharp
// Used in Unit Tests for isolated testing
services.AddScoped<IUserProfileRepository, InMemoryUserProfileRepository>();
```

## Authentication Approaches by Test Layer

### 1. Unit Tests
- **Mock Authentication**: Use `Mock<IAuthenticationService>`
- **No HTTP Headers**: Direct service method calls

### 2. Repository Tests
- **No Authentication**: Direct repository access
- **Test Data**: Create test entities directly

### 3. Integration Tests (All Types)
- **TestAuthenticationService**: Consistent authentication service
- **Bearer Token**: `"Authorization: Bearer test-session-token"`
- **Standard UserId**: `"test_user_id"` for primary test user
- **Additional UserIds**: `"test_user_2"`, `"test_user_3"` for multi-user scenarios

```csharp
// Standard authentication header for all integration tests
_client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", "test-session-token");
```

## Data Management Strategies

### 1. Unit Tests
- **Mock Data**: Create fake objects as needed
- **Isolation**: Each test creates its own data
- **No Persistence**: Data doesn't survive test completion

### 2. Repository Tests
- **Test Objects**: Create real model instances
- **In-Memory Storage**: Data persists during test execution
- **Clean State**: Fresh repository per test

### 3. Integration Tests - Standard Approach
- **API Creation**: Create data via API endpoints
- **HTTP Requests**: Use POST/PUT to create test data
- **Standard UserIds**: Consistent user identification

### 4. Integration Tests - Chat API Approach
- **Direct Repository Population**: Bypass API for user creation
- **Pre-populated Data**: Setup users before test execution
- **Reason**: Avoids Profile API content-type issues

```csharp
// Chat API specific - Direct repository approach
await SetupTestUsersAsync(); // Populates repository directly
// vs Standard approach - API creation
var response = await _client.PostAsJsonAsync("/api/profiles", createRequest);
```

## Bottom-Up Test Fixing Methodology

### Priority Order (Easiest to Hardest)

#### 1. **Unit Tests** (Highest Priority - Fix First)
- **Why First**: Isolated, no dependencies, fastest to run
- **Common Issues**: Mock setup, assertion logic
- **Fix Approach**: 
  - Verify mock configurations
  - Check assertion expectations
  - Validate test data creation

#### 2. **Repository Tests** (Second Priority)
- **Why Second**: Direct repository access, no HTTP layer
- **Common Issues**: Data model mismatches, CRUD logic
- **Fix Approach**:
  - Verify model constructors
  - Check repository method implementations
  - Validate data persistence logic

#### 3. **API Smoke Tests** (Third Priority)
- **Why Third**: Simple API validation, basic scenarios
- **Common Issues**: Authentication headers, basic userId mismatches
- **Fix Approach**:
  - Add missing authentication headers
  - Fix userId consistency
  - Verify basic API endpoints

#### 4. **Core API Integration Tests** (Fourth Priority)
- **Why Fourth**: Standard API workflows, established patterns
- **Common Issues**: Authentication, userId mismatches, data persistence
- **Fix Approach**:
  - Apply authentication fixes from Smoke tests
  - Ensure userId consistency
  - Verify repository configuration

#### 5. **Extended API Integration Tests** (Fifth Priority)
- **Why Fifth**: Complex scenarios building on core functionality
- **Common Issues**: Edge cases, complex data scenarios
- **Fix Approach**:
  - Apply fixes from Core API tests
  - Handle edge case data properly
  - Verify complex workflow logic

#### 6. **Error Handling Integration Tests** (Sixth Priority)
- **Why Sixth**: Specialized error scenarios, boundary conditions
- **Common Issues**: Error response expectations, boundary data
- **Fix Approach**:
  - Apply authentication fixes from other layers
  - Verify error response formats
  - Handle boundary condition data

#### 7. **Chat API Integration Tests** (Lowest Priority - Fix Last)
- **Why Last**: Most complex, requires special setup, multiple dependencies
- **Common Issues**: User profile dependencies, complex data relationships
- **Fix Approach**:
  - Use direct repository population
  - Ensure all dependent users exist
  - Apply all authentication fixes from other layers

### Test Fixing Workflow

```
1. Run Unit Tests → Fix any failures
2. Run Repository Tests → Fix any failures  
3. Run API Smoke Tests → Fix authentication/basic issues
4. Run Core API Integration Tests → Apply smoke test fixes
5. Run Extended API Integration Tests → Apply core fixes
6. Run Error Handling Integration Tests → Apply previous fixes
7. Run Chat API Integration Tests → Apply all previous fixes + special setup
8. Run Full Test Suite → Verify no regressions
```

## Common Patterns and Solutions

### Authentication Issues
```csharp
// Always add this header to integration tests
_client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", "test-session-token");
```

### UserId Consistency
```csharp
// Use these standard userIds across all tests
const string PRIMARY_USER_ID = "test_user_id";
const string SECONDARY_USER_ID = "test_user_2";
const string TERTIARY_USER_ID = "test_user_3";
```

### Repository Configuration
```csharp
// Integration tests - Use Singleton for data persistence
services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();

// Unit tests - Use Scoped for isolation
services.AddScoped<IUserProfileRepository, InMemoryUserProfileRepository>();
```

### Chat API Special Setup
```csharp
// Chat API requires direct repository population
await SetupTestUsersAsync(); // Call before any chat operations
```

## Troubleshooting Guide

### Common Test Failures and Solutions

1. **401 Unauthorized**
   - **Cause**: Missing authentication headers
   - **Solution**: Add Bearer token header to all HTTP requests

2. **400 Bad Request - UserId mismatch**
   - **Cause**: Inconsistent userIds between authentication and requests
   - **Solution**: Use standard "test_user_id" consistently

3. **"Receiver not found" (Chat API)**
   - **Cause**: User profiles don't exist in repository
   - **Solution**: Call `SetupTestUsersAsync()` before chat operations

4. **Data not persisting between requests**
   - **Cause**: Using Scoped instead of Singleton repositories
   - **Solution**: Register repositories as Singleton in WebApplicationFactory

5. **JSON deserialization errors**
   - **Cause**: Model constructor parameter mismatches
   - **Solution**: Verify model constructors match JSON property names

## Best Practices

### When Writing New Tests
1. **Start with Unit Tests**: Test individual components first
2. **Use Standard UserIds**: Always use "test_user_id" for consistency
3. **Add Authentication Headers**: Include Bearer token in all integration tests
4. **Follow Repository Patterns**: Use appropriate repository registration for test type
5. **Document Special Cases**: Note any deviations from standard patterns

### When Fixing Failing Tests
1. **Bottom-Up Approach**: Fix Unit → Repository → Smoke → Core → Extended → Error → Chat
2. **Apply Fixes Systematically**: Use solutions from simpler tests in complex ones
3. **Verify No Regressions**: Run full test suite after major fixes
4. **Document Root Causes**: Update this document with new patterns discovered

### When Adding New Features
1. **Write Unit Tests First**: Test business logic in isolation
2. **Add Repository Tests**: Verify data persistence
3. **Create Integration Tests**: Test end-to-end workflows
4. **Follow Existing Patterns**: Use established authentication and data patterns
5. **Update Documentation**: Add new patterns to this guide

This architecture ensures maintainable, reliable tests that provide comprehensive coverage while following consistent patterns and enabling efficient troubleshooting and development workflows.
