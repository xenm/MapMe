# Development Documentation

This section contains comprehensive development guidelines for MapMe, including coding standards, workflows, and development environment setup.

## Quick Navigation

| Document | Purpose |
|----------|----------|
| [AI Coding Assistant Rulebook](./ai-coding-assistant-rulebook.md) | **Versioned development standards for AI-assisted development** |
| [Coding Standards](./coding-standards.md) | Code style and conventions |
| [Git Workflow](./git-workflow.md) | Branching strategies and commit guidelines |
| [Code Review](./code-review.md) | Code review processes and checklists |
| [Debugging](./debugging.md) | Debugging techniques and tools |
| [Local Services](./local-services.md) | Running dependencies locally |
| [Database Setup](./database-setup.md) | Local database configuration |
| [Environment Variables](./environment-variables.md) | Configuration management |
| [Development Tools](./tools.md) | Recommended development tools |
| [Contributing](./contributing.md) | Contribution guidelines |

## Development Overview

MapMe follows modern .NET development practices with emphasis on:

### Core Principles
- **Clean Architecture**: Clear separation of concerns
- **SOLID Principles**: Object-oriented design best practices
- **Test-Driven Development**: Comprehensive test coverage
- **Secure by Design**: Security considerations in all features
- **Performance First**: Optimized code and efficient algorithms

### Technology Standards
- **.NET 10 Preview**: Latest .NET framework features
- **System.Text.Json**: Exclusive JSON serialization
- **Nullable Reference Types**: Enabled for null safety
- **Async/Await**: Non-blocking I/O operations throughout
- **Repository Pattern**: Clean data access abstraction

## Development Environment

### Required Tools
- **.NET 10 SDK**: Latest preview version
- **IDE**: JetBrains Rider (preferred) or Visual Studio 2022
- **Git**: Version control system
- **Docker**: Container development (optional)
- **Postman**: API testing (recommended)

### Recommended Extensions
- **C# Dev Kit**: Enhanced C# development (VS Code)
- **GitLens**: Git integration and history
- **SonarLint**: Code quality analysis
- **Thunder Client**: API testing (VS Code alternative to Postman)

## Code Quality Standards

### Coding Conventions
- **C# Coding Conventions**: Follow Microsoft C# coding standards
- **Naming Conventions**: PascalCase for public members, camelCase for private
- **File Organization**: One class per file, logical folder structure
- **Documentation**: XML documentation for public APIs

### Code Analysis
- **Static Analysis**: SonarCloud integration for code quality
- **Security Scanning**: Automated vulnerability detection
- **Performance Analysis**: Profiling and optimization recommendations
- **Test Coverage**: Minimum 80% code coverage requirement

## Development Workflow

### Feature Development
1. **Create Feature Branch**: `feature/feature-name` from `main`
2. **Implement Feature**: Following TDD principles
3. **Write Tests**: Unit and integration tests
4. **Code Review**: Peer review process
5. **Merge to Main**: After approval and CI success

### Bug Fixes
1. **Create Bug Branch**: `bugfix/issue-description` from `main`
2. **Reproduce Issue**: Write failing test first
3. **Fix Implementation**: Minimal change to resolve issue
4. **Verify Fix**: Ensure tests pass and no regressions
5. **Merge to Main**: After review and validation

### Hotfixes
1. **Create Hotfix Branch**: `hotfix/critical-issue` from `main`
2. **Implement Fix**: Minimal, targeted change
3. **Emergency Review**: Expedited review process
4. **Deploy Immediately**: Direct to production after testing

## Testing Requirements

### Test Coverage
- **Unit Tests**: All business logic and services
- **Integration Tests**: API endpoints and data access
- **End-to-End Tests**: Critical user workflows
- **Performance Tests**: Load and stress testing

### Test Quality
- **Descriptive Names**: Clear test intent and scenarios
- **Independent Tests**: No test dependencies or ordering
- **Fast Execution**: Quick feedback for development
- **Reliable Results**: Consistent test outcomes

## Security Guidelines

### Secure Coding Practices
- **Input Validation**: Validate all user input
- **Output Encoding**: Prevent XSS attacks
- **Authentication**: Proper JWT implementation
- **Authorization**: Role-based access control
- **Logging**: Secure logging without sensitive data

### Security Reviews
- **Code Review**: Security-focused code reviews
- **Dependency Scanning**: Automated vulnerability scanning
- **Penetration Testing**: Regular security assessments
- **Security Training**: Ongoing developer education

## Performance Guidelines

### Performance Best Practices
- **Async Operations**: Non-blocking I/O throughout
- **Database Optimization**: Efficient queries and indexing
- **Caching Strategy**: Appropriate caching implementation
- **Resource Management**: Proper disposal and cleanup
- **Memory Efficiency**: Minimize allocations and garbage collection

### Performance Monitoring
- **Profiling**: Regular performance profiling
- **Metrics Collection**: Application performance metrics
- **Load Testing**: Performance under load
- **Optimization**: Continuous performance improvements

## Documentation Standards

### Code Documentation
- **XML Comments**: Public APIs and complex logic
- **README Files**: Project and component overviews
- **Architecture Decisions**: Document significant decisions
- **API Documentation**: Comprehensive endpoint documentation

### Documentation Maintenance
- **Keep Current**: Update docs with code changes
- **Review Process**: Documentation review in code reviews
- **User Feedback**: Incorporate user feedback on documentation
- **Regular Audits**: Periodic documentation quality reviews

## Related Documentation

- [Getting Started](../getting-started/README.md) - Initial setup and onboarding
- [Architecture](../architecture/README.md) - System architecture and design
- [Testing](../testing/README.md) - Testing strategies and practices
- [Deployment](../deployment/README.md) - Deployment and infrastructure

---

**Last Updated**: 2025-08-30  
**Code Quality**: SonarCloud Grade A  
**Maintained By**: Development Team
