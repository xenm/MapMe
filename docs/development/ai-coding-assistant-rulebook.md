# AI Coding Assistant Rulebook

**Version**: 1.0  
**Last Updated**: August 31, 2025  
**Applies To**: MapMe .NET 10 Dating Application

## Recommended Development Environment

### IDE and AI Assistant Setup
- **Primary IDE**: JetBrains Rider (latest version)
- **AI Assistant**: Cascade extension with Claude Sonnet 4 or newer
- **Why This Combination**:
  - Rider provides excellent .NET 10 support and debugging capabilities
  - Cascade extension offers superior code analysis and generation
  - Claude Sonnet 4+ ensures latest AI capabilities for complex architectural decisions
  - Integrated workflow for AI-assisted development following these rules

### Alternative Setups
- **Windsurf IDE** (VSCode fork) with built-in AI capabilities (excellent alternative)
- **Visual Studio 2022** with GitHub Copilot (acceptable alternative)
- **VS Code** with C# Dev Kit + AI extensions (for lighter development)

## Copyable Rulebook Content

```markdown
# AI Coding Assistant Rulebook

## Core Rules (ALWAYS FOLLOW):

### 1. Technology Stack
- **Use .NET 10** features and APIs wherever possible
- **Follow .NET best practices** in all code implementations
- **Use System.Text.Json** instead of Newtonsoft.Json for all JSON operations
- **NEVER add Newtonsoft.Json** as a dependency - the project has custom serializers to avoid it

### 2. Code Quality and Security
- **Hold strong code quality standards** and security practices
- Write clean, readable, maintainable code following SOLID principles
- Implement proper error handling and logging
- Use async/await patterns correctly
- **Code quality analyzers should not produce false positives**

### 3. Error Handling and Logging Standards
- **NEVER log sensitive information** - use SecureLogging utilities (see [MapMe.Utilities.SecureLogging](cci:2://file:///Users/adamzaplatilek/GitHub/MapMe/MapMe/MapMe/Utilities/SecureLogging.cs:10:0-183:1))
- **Always use sanitization helpers** from [SecureLogging](cci:2://file:///Users/adamzaplatilek/GitHub/MapMe/MapMe/MapMe/Utilities/SecureLogging.cs:10:0-183:1) class:
    - [SanitizeForLog()](cci:1://file:///Users/adamzaplatilek/GitHub/MapMe/MapMe/MapMe/Utilities/SecureLogging.cs:15:4-48:5) for general user input
    - [SanitizeUserIdForLog()](cci:1://file:///Users/adamzaplatilek/GitHub/MapMe/MapMe/MapMe/Utilities/SecureLogging.cs:116:4-134:5) for user identifiers
    - [ToTokenPreview()](cci:1://file:///Users/adamzaplatilek/GitHub/MapMe/MapMe/MapMe/Utilities/SecureLogging.cs:50:4-70:5) for JWT tokens (never log full tokens)
    - [SanitizeEmailForLog()](cci:1://file:///Users/adamzaplatilek/GitHub/MapMe/MapMe/MapMe/Utilities/SecureLogging.cs:72:4-89:5) for email addresses
    - [SanitizeHeaderForLog()](cci:1://file:///Users/adamzaplatilek/GitHub/MapMe/MapMe/MapMe/Utilities/SecureLogging.cs:91:4-114:5) for HTTP headers
- **Follow secure logging policy** documented in [docs/security/secure-logging.md](cci:7://file:///Users/adamzaplatilek/GitHub/MapMe/docs/security/secure-logging.md:0:0-0:0)
- **Use structured logging** with Serilog and proper log levels
- **Handle exceptions gracefully** with appropriate error responses
- **Log authentication events** safely without exposing credentials

### 4. Security and Secrets Management
- **NEVER commit real secrets** to version control in any file
- **Documentation and appsettings.json** must have nothing that looks like real secrets
- **appsettings.Development.json** should only contain:
    - Clear placeholders (e.g., "YOUR_API_KEY_HERE")
    - Publicly known secrets from official Microsoft guides (e.g., Cosmos DB emulator keys)
    - No real API keys, connection strings, or sensitive data
- **appsettings.Development.json will be gitignored** for new developers to prevent security breaches
- **Use User Secrets or environment variables** for real configuration in development/production

### 5. Documentation Management
- **Keep documentation up to date** following docs/ folder structure and organization
- **Always read existing documentation FIRST** before making any changes
- **Search the codebase** for existing implementations before creating new ones
- Update relevant documentation after code changes

### 6. Documentation Location Rules
- **CHANGELOG.md ONLY** for all change documentation ("what changed")
- **docs/ folder ONLY** for current implementation and how-to guides ("how it works")
- **NEVER create new markdown in root folder** except project-level files (README.md, CHANGELOG.md, CONTRIBUTING.md)
- **ALWAYS extend existing docs files** instead of creating new ones when content fits existing structure
- **Follow docs/ folder hierarchy**: backend/, frontend/, api/, security/, testing/, deployment/, etc.

### 7. Research and Analysis Process
- **Always search documentation and codebase** before implementing solutions
- **Read existing code patterns** and follow established conventions
- **Check for custom implementations** before adding dependencies
- **Verify assumptions** by examining actual code rather than making assumptions

### 8. Testing Strategy and Test Pyramid Best Practices
- **Follow test pyramid hierarchy** - write more unit tests than integration tests, more integration tests than end-to-end tests
- **Build and run all tests after each change** to verify correctness
- **Add comprehensive tests** for each new feature or bug fix following these guidelines:

#### Unit Tests (Foundation - Most Tests)
- **Test individual methods and classes** in isolation
- **Use mocking frameworks** (e.g., Moq, NSubstitute) to isolate dependencies
- **Focus on business logic** and edge cases
- **Fast execution** - should run in milliseconds
- **No external dependencies** (databases, APIs, file system)
- **High coverage** of business logic and utility functions
- **Test naming convention**: MethodName_Scenario_ExpectedResult

**For Blazor Components:**
- **Use bUnit** for Blazor component unit testing
- **Test component rendering** and parameter binding
- **Mock component dependencies** and services
- **Test component lifecycle** events and state changes
- **Verify component markup** and CSS class assignments

**For Flutter Widgets:**
- **Use flutter_test package** for widget unit testing
- **Test widget properties** and state management
- **Mock external dependencies** using mockito or similar
- **Test widget interactions** like taps, scrolling, form input
- **Verify widget tree structure** and rendered output

#### Integration Tests (Middle Layer - Moderate Tests)
- **Test component interactions** within the application
- **Test API controllers** with real service implementations
- **Test database operations** with test database or in-memory providers
- **Verify configuration and dependency injection** setup
- **Test cross-cutting concerns** like authentication, authorization, logging
- **Use TestContainers** for external service dependencies when needed
- **Moderate execution time** - should complete within seconds

**For Blazor Applications:**
- **Test Blazor Server** SignalR connections and circuit management
- **Test Blazor WebAssembly** JavaScript interop and API communication
- **Test authentication flows** with TestServer and WebApplicationFactory
- **Verify routing and navigation** between Blazor pages
- **Test dependency injection** resolution in Blazor components

**For Flutter Applications:**
- **Use integration_test package** for Flutter integration testing
- **Test navigation flows** between screens and routes
- **Test state management** solutions (Provider, Bloc, Riverpod)
- **Test platform channels** and native plugin interactions
- **Verify API integration** with real or mock HTTP clients

#### End-to-End Tests (Top Layer - Fewest Tests)
- **Test critical user journeys** and business scenarios
- **Test full application stack** including UI, API, and database
- **Focus on high-value workflows** that represent core business functionality
- **Use browser automation** (e.g., Playwright, Selenium) for frontend testing
- **Test deployment scenarios** and production-like environments
- **Slowest execution** - acceptable to run in minutes
- **Minimal but comprehensive** - cover happy paths and critical error scenarios

**For Blazor Applications:**
- **Use Playwright or Selenium** for Blazor Server and WebAssembly E2E testing
- **Test complete user workflows** across multiple Blazor components and pages
- **Verify real-time features** like SignalR notifications in Blazor Server
- **Test progressive web app** features for Blazor WebAssembly
- **Cross-browser compatibility** testing for Blazor applications

**For Flutter Applications:**
- **Use flutter_driver or integration_test** for full app testing
- **Test on real devices** and emulators for platform-specific behavior
- **Test app lifecycle** events (background, foreground, termination)
- **Verify platform-specific features** like permissions, notifications, deep linking
- **Performance testing** for animations, scrolling, and memory usage

#### Test Organization and Maintenance
- **Organize tests by layer** in separate projects (Unit.Tests, Integration.Tests, E2E.Tests)
- **Use test data builders** and object mothers for consistent test data creation
- **Implement test cleanup** to ensure test isolation
- **Maintain test performance** - regularly review and optimize slow tests
- **Parallel test execution** where possible to reduce overall test suite time
- **Test environment parity** - ensure test environments match production as closely as possible

**Cross-Platform Testing Standards:**
- **Blazor**: Use bUnit for components, WebApplicationFactory for integration, Playwright for E2E
- **Flutter**: Use flutter_test for widgets, integration_test for flows, flutter_driver for full app testing
- **Shared .NET Backend**: Apply same unit/integration testing principles regardless of frontend technology
- **Golden file testing** for UI regression testing (Flutter) and visual component testing (Blazor with bUnit)
- **Snapshot testing** for component output verification across platforms

## Documentation Decision Tree:
1. **Is it a change/fix?** → CHANGELOG.md
2. **Is it current implementation?** → Appropriate docs/ subfolder
3. **Does existing docs file cover this topic?** → Extend existing file
4. **Is it new technical area?** → Create in appropriate docs/ subfolder
5. **Never create root-level markdown** except project files

## Decision Making Process:
1. **Read documentation** in docs/ folder first
2. **Search codebase** for existing patterns and implementations
3. **Follow established conventions** rather than creating new approaches
4. **Test changes** immediately after implementation
5. **Update documentation** to reflect changes

## Key Principles:
- Read first, then implement
- Follow existing patterns
- Test immediately
- Document changes appropriately
- Never add unnecessary dependencies
- Extend existing docs instead of creating new files
- Never commit real secrets
- Use clear placeholders only
- Always sanitize logs with SecureLogging utilities
- Never log sensitive user information
```
