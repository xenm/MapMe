# AI Coding Assistant Rulebook (Always On)

## CRITICAL RULES (Never Break These)

### Technology & Dependencies

- **USE**: .NET 10, System.Text.Json, established project patterns
- **NEVER**: Add Newtonsoft.Json or unnecessary dependencies
- **PLATFORM TARGETING**: Follow platform-specific patterns for Blazor WASM vs MAUI

### Security & Logging (ZERO TOLERANCE)

- **ALWAYS** use `SecureLogging` utilities for ALL log entries:
    - `SanitizeForLog()` - user input
    - `SanitizeUserIdForLog()` - user IDs
    - `ToTokenPreview()` - tokens (never full tokens)
    - `SanitizeEmailForLog()` - emails
    - `SanitizeHeaderForLog()` - HTTP headers
- **NEVER** log sensitive data directly
- **NEVER** commit real secrets anywhere
- **ONLY** use clear placeholders in `appsettings.Development.json`
- **PREVENT** common vulnerabilities:
    - SQL injection: Use parameterized queries/EF Core only
    - XSS: Always encode output, validate input
    - Path traversal: Validate file paths, no direct user input
    - Deserialization attacks: Avoid untrusted deserialization
    - CSRF: Use anti-forgery tokens
    - Weak crypto: Use strong hashing (SHA256+), proper salt
    - Resource leaks: Always dispose IDisposable (using statements)
    - Null reference exceptions: Use null-conditional operators

### Research Process (Do This First)

1. **Search existing docs/** folder
2. **Search codebase** for existing implementations
3. **Follow established patterns** - don't create new approaches
4. **Extend existing files** instead of creating new ones

## CODE STANDARDS

### Quality Requirements

- Follow SOLID principles and .NET best practices
- **One class per file** (mandatory)
- **Follow established naming and folder conventions** exactly
- Use async/await correctly
- Implement proper error handling
- Write clean, maintainable code

### Data Handling & UI Integrity

- **NEVER display fake or hardcoded data**:
    - **ALWAYS** implement proper loading states (e.g., skeleton loaders, spinners) while data is being fetched.
    - If a data fetch returns an empty collection, **ALWAYS** display a clear "empty state" message (e.g., "No items
      found.").
    - **NEVER** leave the UI in a broken or perpetual loading state.
- **Implement robust retry and fallback policies**:
    - For transient network errors, **ALWAYS** implement a retry policy, preferably with exponential backoff (e.g.,
      using Polly).
    - If data fetching fails after all retries, provide a fallback. This can be serving stale data from a cache (if
      acceptable) or displaying a user-friendly error state.
- **Provide user-driven recovery actions**:
    - When a data fetch fails permanently (after retries), the UI **MUST** display a clear error message and a "Retry"
      or "Refresh" button to allow the user to initiate the action again.
- **Follow data flow best practices**:
    - **Separation of Concerns**: Keep data fetching logic in dedicated services, separate from UI components (Blazor)
      or Views/ViewModels (MAUI).
    - **Unidirectional Data Flow**: State should flow down from services to UI components. Events and actions should
      flow up from the UI to the services.
    - **Immutability**: Use records or immutable DTOs for state and API models to prevent unintended side effects.

### Platform-Specific Rules

#### Blazor (Server & WebAssembly)

- **Memory Management**: Dispose event handlers, avoid circular references
- **State Management**: Use proper `StateHasChanged()` calls
- **JS Interop**: Always check if disposed before JS calls
- **Performance**: Minimize re-renders, use `@key` for dynamic lists
- **Security**: Validate all user input, never trust client-side data

#### MAUI (Cross-Platform)

- **Memory Leaks**: Always unsubscribe events, dispose views properly
- **Navigation**: Use Shell navigation, avoid memory-leaking patterns
- **Platform APIs**: Use conditional compilation for platform-specific code
- **Performance**: Optimize CollectionView, use virtualization
- **Resources**: Dispose graphics resources, use weak references

### Testing Strategy (Test Pyramid)

- **Unit Tests** (Most): Fast, isolated, mock dependencies, high coverage
- **Integration Tests** (Moderate): Component interactions, real services, seconds to run
- **E2E Tests** (Fewest): Critical user journeys, full stack, minutes to run
- **ALWAYS** build and run tests after changes
- **Platform Testing**:
    - Blazor: Use bUnit for components, TestServer for integration
    - MAUI: Use device/simulator testing, platform-specific tests

## DOCUMENTATION RULES

### File Location (Strict)

- **Changes/fixes** → `CHANGELOG.md` ONLY
- **Temporary changes for development that will need undoing** → `docs/TODO.md` ONLY
- **Implementation docs** → `docs/` folder ONLY
- **ALWAYS** keep README.md files up to date in all folders
- **NEVER** create root-level markdown except README.md, CHANGELOG.md, CONTRIBUTING.md, SECURITY.md
- **ALWAYS** extend existing docs files when content fits

### Decision Tree

```
Change/fix? → CHANGELOG.md
Current implementation? → docs/ subfolder
Topic exists in docs/? → Extend existing file
New technical area? → Create in docs/ subfolder
```

## WORKFLOW CHECKLIST

### Before Coding

- [ ] Read relevant docs/ files
- [ ] Search codebase for existing patterns
- [ ] Verify no existing implementation exists
- [ ] **Check platform-specific requirements** (Blazor/MAUI)

### While Coding

- [ ] Follow established conventions and naming patterns exactly
- [ ] One class per file (no exceptions)
- [ ] Use SecureLogging for ALL logging
- [ ] **Data Integrity**: Implement loading, empty, and error states. Never show fake data.
- [ ] **Resiliency**: Add retry/fallback policies for data fetching.
- [ ] **Memory Management**: Dispose resources, unsubscribe events
- [ ] **Performance**: Avoid memory leaks, optimize for target platform
- [ ] **Security**: Parameterized queries, encode output, validate input, use `using` statements
- [ ] Never add unnecessary dependencies
- [ ] Write tests following pyramid strategy

### After Coding

- [ ] Run all tests (unit, integration, platform-specific)
- [ ] **Memory Testing**: Check for leaks in MAUI/Blazor apps
- [ ] Update CHANGELOG.md for changes
- [ ] Update docs/ for new implementations
- [ ] Verify no secrets committed

## QUICK REFERENCE

**Security Logging Pattern:**

```csharp
_logger.LogInformation("User action: {UserId}, Input: {Input}", 
    SecureLogging.SanitizeUserIdForLog(userId),
    SecureLogging.SanitizeForLog(userInput));
```

**Data Loading UI State Pattern:**

```csharp
// Blazor Example
@if (isLoading)
{
    <Spinner />
}
else if (loadFailed)
{
    <ErrorMessage Message="Failed to load data.">
        <RetryButton OnClick="LoadDataAsync" />
    </ErrorMessage>
}
else if (!items.Any())
{
    <EmptyStateMessage Message="No items found." />
}
else
{
    // Render items
}
```

**Memory Management Patterns:**

```csharp
// ✅ Blazor: Proper disposal
public void Dispose()
{
    if (_timer != null)
    {
        _timer.Dispose();
        _timer = null;
    }
}

// ✅ MAUI: Unsubscribe events
protected override void OnDisappearing()
{
    MyEvent -= OnMyEvent;
    base.OnDisappearing();
}
```

**Security Pattern:**

```csharp
// ✅ Good: Parameterized query
var users = await context.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// ✅ Good: Proper disposal
using var httpClient = new HttpClient();

// ✅ Good: Input validation
if (string.IsNullOrWhiteSpace(userInput) || userInput.Length > 100)
    throw new ArgumentException("Invalid input");
```

**Test Naming:** `MethodName_Scenario_ExpectedResult`

**Documentation:** Extend existing \> Create new

**File Organization:** One class per file, follow exact naming/folder conventions

**Dependencies:** System.Text.Json \> Newtonsoft.Json

**Platform Performance:** Dispose resources, optimize re-renders, use weak references

-----

*Key Principle: Read first, follow patterns, test immediately, document appropriately, manage memory carefully*