# Contributing to MapMe

Thank you for your interest in contributing to MapMe! This document provides guidelines and information for contributors.

## License Agreement
By contributing code to MapMe, you agree that your contributions will be licensed under the same [PolyForm Noncommercial License 1.0.0](LICENSE) as the project.

## What You Can Contribute
- ✅ Bug fixes and improvements
- ✅ New features that enhance the platform
- ✅ Documentation improvements
- ✅ Testing and quality assurance

## What This Means
- Your contributions become part of the noncommercial codebase
- Adam Zaplatílek retains the right to offer commercial licenses
- You retain attribution for your specific contributions
- All contributions must respect the noncommercial nature of the project

## How to Contribute
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request
5. Ensure your code follows the project standards

Questions about licensing? Contact [💬 Adam Zaplatílek](mailto:adam.zaplatilek@gmail.com)

## Getting Started

### Prerequisites
- .NET 10 SDK (preview)
- Git
- Modern IDE (Visual Studio, Rider, or VS Code)
- Google Maps API key for testing

### Development Setup
1. **Fork and Clone**
   ```bash
   git clone https://github.com/yourusername/MapMe.git
   cd MapMe
   ```

2. **Install Dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure Secrets**
   ```bash
   cd MapMe/MapMe/MapMe
   dotnet user-secrets init
   dotnet user-secrets set "GoogleMaps:ApiKey" "your-google-api-key"
   dotnet user-secrets set "GoogleAuth:ClientId" "your-google-oauth-client-id"
   dotnet user-secrets set "Jwt:SecretKey" "your-jwt-secret-key-minimum-32-characters"
   ```

4. **Run the Application**
   ```bash
   dotnet run --project MapMe/MapMe/MapMe.csproj
   ```

5. **Verify Setup**
   - Navigate to `https://localhost:8008`
   - Verify map loads and basic functionality works

## Development Guidelines

### Code Standards
- **Follow .NET Conventions**: Use standard C# naming conventions and code style
- **Use System.Text.Json**: Prefer System.Text.Json over Newtonsoft.Json
- **Nullable Reference Types**: Enable and properly handle nullable reference types
- **Async/Await**: Use proper async patterns for I/O operations
- **Dependency Injection**: Follow established DI patterns in the codebase

### Architecture Principles
- **Clean Architecture**: Maintain separation of concerns
- **Repository Pattern**: Use established repository interfaces
- **Service Layer**: Business logic belongs in service classes
- **Client-Server Separation**: Clear boundaries between Blazor client and ASP.NET Core server

### Testing Requirements
- **Unit Tests**: Write unit tests for new business logic
- **Integration Tests**: Add integration tests for new API endpoints
- **Test Coverage**: Maintain high test coverage (currently 300/300 tests passing)
- **Test Naming**: Use descriptive test method names following Given_When_Then pattern

### Security Guidelines
- **Secure Logging**: Use `SecureLogging.SanitizeForLog()` for user input
- **JWT Handling**: Never log raw JWT tokens, use `ToTokenPreview()`
- **Input Validation**: Validate all user inputs on both client and server
- **API Security**: Ensure proper authentication and authorization

## Contribution Process

### 1. Issue Creation
- **Bug Reports**: Use the bug report template with reproduction steps
- **Feature Requests**: Describe the feature and its business value
- **Documentation**: Identify gaps or improvements needed

### 2. Development Workflow
```bash
# Create feature branch
git checkout -b feature/your-feature-name

# Make changes and commit
git add .
git commit -m "feat: add new feature description"

# Push and create PR
git push origin feature/your-feature-name
```

### 3. Pull Request Guidelines
- **Clear Title**: Descriptive title following conventional commits
- **Description**: Explain what changes were made and why
- **Testing**: Include test results and manual testing notes
- **Documentation**: Update relevant documentation
- **Breaking Changes**: Clearly mark any breaking changes

### 4. Code Review Process
- **Automated Checks**: Ensure all CI/CD checks pass
- **Peer Review**: At least one code review required
- **Testing**: Verify tests pass and coverage is maintained
- **Documentation**: Ensure documentation is updated

## Development Areas

### High-Priority Areas
- **Performance Optimization**: Map rendering, data loading, caching
- **Mobile Experience**: Touch interactions, responsive design
- **Accessibility**: WCAG compliance, keyboard navigation
- **Security Enhancements**: Authentication, authorization, data protection

### Feature Development
- **Real-time Features**: SignalR integration for live updates
- **Social Features**: Friend systems, activity feeds
- **Advanced Maps**: Heatmaps, custom overlays, offline support
- **Analytics**: User behavior tracking, performance metrics

### Infrastructure Improvements
- **CI/CD Pipeline**: GitHub Actions, deployment automation
- **Monitoring**: Application insights, error tracking
- **Documentation**: API documentation, user guides
- **Testing**: E2E testing, performance testing

## Testing Guidelines

### Running Tests
```bash
# Unit tests only
dotnet test MapMe/MapMe/MapMe.Tests --filter "Category=Unit"

# Integration tests only
dotnet test MapMe/MapMe/MapMe.Tests --filter "Category!=Unit"

# All tests
dotnet test MapMe/MapMe/MapMe.Tests
```

### Test Categories
- **Unit Tests**: Fast, isolated tests for business logic
- **Integration Tests**: API endpoint testing with in-memory repositories
- **Manual Tests**: UI/UX testing checklist for map functionality

### Writing Tests
- **Arrange-Act-Assert**: Clear test structure
- **Descriptive Names**: Test names should describe the scenario
- **Test Data**: Use realistic test data that reflects actual usage
- **Cleanup**: Ensure tests don't leave side effects

## Documentation Standards

### Code Documentation
- **XML Comments**: Public APIs should have XML documentation
- **README Updates**: Update README.md for significant changes
- **Architecture Docs**: Update architecture documentation for structural changes

### User Documentation
- **Getting Started**: Keep setup instructions current
- **Feature Guides**: Document new features with examples
- **Troubleshooting**: Add common issues and solutions

## Communication

### Channels
- **GitHub Issues**: Bug reports, feature requests, discussions
- **Pull Requests**: Code review and collaboration
- **Documentation**: Questions about architecture or implementation

### Guidelines
- **Be Respectful**: Professional and constructive communication
- **Be Specific**: Provide detailed information and context
- **Be Patient**: Allow time for review and feedback

## Recognition

Contributors will be recognized in:
- **CONTRIBUTORS.md**: List of all contributors
- **Release Notes**: Major contributions highlighted
- **GitHub**: Contributor statistics and activity

## Questions?

If you have questions about contributing:
1. Check the [documentation](./docs/README.md)
2. Search existing [GitHub issues](https://github.com/xenm/MapMe/issues)
3. Create a new issue with the "question" label

Thank you for contributing to MapMe! 🗺️💕
