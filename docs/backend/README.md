# Backend Documentation

This section contains comprehensive documentation for MapMe's .NET 10 backend implementation, including services, repositories, authentication, and data access patterns.

## Quick Navigation

| Document | Purpose |
|----------|---------|
| [Getting Started](./getting-started.md) | Backend-specific setup and development |
| [Project Structure](./project-structure.md) | Codebase organization and conventions |
| [Configuration](./configuration.md) | Configuration management and secrets |
| [Authentication](./authentication.md) | JWT authentication implementation |
| [Authorization](./authorization.md) | Role-based access control |
| [Data Access](./data-access/README.md) | Repository pattern and database integration |
| [Services](./services/README.md) | Business logic and service layer |
| [Middleware](./middleware/README.md) | Custom middleware documentation |
| [Background Jobs](./background-jobs.md) | Scheduled tasks and background processing |
| [Caching](./caching.md) | Caching strategies and implementation |
| [Logging](./logging.md) | Structured logging with Serilog |
| [Performance](./performance.md) | Performance optimization techniques |

## Technology Stack

### Core Framework
- **.NET 10 Preview**: Latest .NET framework with preview features
- **ASP.NET Core**: Web API and hosting framework
- **System.Text.Json**: JSON serialization (exclusively, no Newtonsoft.Json)
- **Minimal APIs**: Lightweight API endpoints

### Data Access
- **Repository Pattern**: Clean architecture with abstraction
- **Azure Cosmos DB**: Production NoSQL database
- **In-Memory Repositories**: Development and testing
- **Custom Serialization**: SystemTextJsonCosmosSerializer

### Authentication & Security
- **JWT Authentication**: Stateless token-based auth
- **User Secrets**: Development configuration management
- **Secure Logging**: No sensitive data in logs
- **Input Validation**: Comprehensive validation throughout

### External Integrations
- **Google Maps API**: Location services integration
- **Google OAuth**: Social authentication
- **Azure Services**: Cloud infrastructure integration

## Key Services

### Core Business Services
- **UserProfileService**: User profile and DateMark management
- **AuthenticationService**: User authentication and JWT handling
- **ChatService**: Real-time messaging functionality
- **LocationService**: Geographic and mapping operations

### Infrastructure Services
- **JwtService**: Token generation and validation
- **EmailService**: Email notifications (future)
- **FileStorageService**: Image and file management (future)
- **CacheService**: Distributed caching implementation

## Repository Pattern

### Interface Abstractions
- **IUserProfileRepository**: User profile data access
- **IDateMarkByUserRepository**: DateMark data with filtering
- **IChatMessageRepository**: Chat message persistence
- **IConversationRepository**: Conversation management

### Implementations
- **In-Memory**: Fast development and testing
- **Cosmos DB**: Production-ready with geospatial queries
- **Future**: SQL Server, PostgreSQL support

## API Design Principles

### RESTful Design
- **Resource-based URLs**: Clear, predictable endpoint structure
- **HTTP Verbs**: Proper use of GET, POST, PUT, DELETE
- **Status Codes**: Meaningful HTTP status code responses
- **Content Negotiation**: JSON-first with extensibility

### Error Handling
- **Consistent Error Format**: Standardized error responses
- **Validation Errors**: Detailed field-level validation feedback
- **Exception Handling**: Global exception handling middleware
- **Logging Integration**: Comprehensive error logging

### Performance
- **Async/Await**: Non-blocking I/O operations throughout
- **Response Caching**: Appropriate caching headers
- **Pagination**: Large dataset handling
- **Compression**: Response compression for efficiency

## Development Workflow

### Local Development
1. **Prerequisites**: .NET 10 SDK, development tools
2. **Configuration**: User Secrets for API keys
3. **Database**: In-memory repositories for fast development
4. **Testing**: Comprehensive unit and integration tests

### Code Quality
- **Nullable Reference Types**: Enabled for null safety
- **Code Analysis**: Static analysis with SonarCloud
- **Unit Testing**: High test coverage with xUnit
- **Integration Testing**: End-to-end API testing

## Related Documentation

- [Architecture Overview](../architecture/system-overview.md) - System architecture
- [API Documentation](../api/README.md) - API endpoints and usage
- [Security Documentation](../security/README.md) - Security implementation
- [Testing Documentation](../testing/README.md) - Testing strategies
- [Deployment Documentation](../deployment/README.md) - Deployment procedures

---

**Last Updated**: 2025-08-30  
**Maintained By**: Backend Development Team  
**Review Schedule**: Monthly
