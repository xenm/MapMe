# Changelog

All notable changes to the MapMe project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **AI Coding Assistant Rulebook**: Versioned development standards for AI-assisted development with Rider + Cascade + Claude Sonnet 4+ recommendations
- Comprehensive documentation restructure with professional navigation
- Complete testing strategy with 300/300 tests passing
- JWT authentication with token refresh capabilities
- Google OAuth integration for secure user authentication
- Real-time chat functionality with conversation management
- Date Mark editing capabilities with comprehensive form validation
- Google Maps links integration in popups and lists
- User profile management with Tinder-style dating fields
- Activity statistics dashboard with real-time metrics
- Docker deployment support with multi-stage builds
- AI Coding Assistant Rulebook for development standards and best practices
- JWT configuration rationale and best practices documentation
- JWT issuer/audience architecture guidelines for monolithic vs microservices

### Fixed
- **Configuration Key Mismatch**: Fixed Program.cs to use correct `CosmosDb:*` configuration keys instead of `Cosmos:*`
- **Production Safety**: Added mandatory Cosmos DB validation in production environment - application fails startup if not configured
- **Placeholder Detection**: Enhanced configuration validation to reject placeholder values and properly fallback to in-memory repositories
- **Test Environment**: Corrected repository selection logic ensuring tests use in-memory repositories while production requires Cosmos DB
- **Build Warnings**: Removed BuildServiceProvider warning from startup configuration
- Cosmos DB integration with geospatial query capabilities
- Secure logging policy with sanitization helpers

### Changed
- **JWT Security Enhancement**: Updated JWT issuer/audience configuration for proper client-server architecture separation
  - Issuer: "MapMe" → "MapMe-Server" (identifies ASP.NET Core server as token issuer)
  - Audience: "MapMe" → "MapMe-Client" (identifies Blazor WebAssembly client as intended recipient)
  - Improved security boundaries and token scope validation
  - Enhanced scalability for future mobile apps and admin interfaces

### Enhanced
- Map functionality with clustering and performance optimization
- Profile pages with unified layout between viewing and editing
- Authentication flow with proper navigation and error handling
- API endpoints with comprehensive error handling and validation
- Frontend architecture with Blazor WebAssembly and Interactive SSR
- Testing infrastructure with unit, integration, and manual testing guides

### Security
- Implemented secure logging practices with JWT token sanitization
- Added input validation and XSS protection
- Enhanced authentication with proper session management
- API key security with domain restrictions and environment-based configuration

## [1.0.0] - Initial Release

### Added
- Core MapMe application with Google Maps integration
- Interactive map with Date Mark creation and management
- User profile system with photo management
- Blazor WebAssembly frontend with ASP.NET Core backend
- Repository pattern with in-memory and Cosmos DB implementations
- Comprehensive test suite with high coverage
- CI/CD pipeline with automated testing and deployment
- Documentation system with architectural guides

### Features
- **Interactive Maps**: Google Maps integration with place search and details
- **Date Marks**: Location-based memory system with ratings and categories
- **User Profiles**: Comprehensive dating app-style profiles with photos
- **Authentication**: JWT-based authentication with Google OAuth support
- **Chat System**: Real-time messaging between users
- **Social Discovery**: Browse other users' profiles and Date Marks
- **Mobile Support**: Responsive design optimized for mobile devices

### Technical
- **.NET 10**: Latest .NET framework with preview features
- **Blazor WebAssembly**: Modern client-side web framework
- **System.Text.Json**: High-performance JSON serialization
- **Azure Cosmos DB**: Scalable NoSQL database with geospatial queries
- **Docker Support**: Containerized deployment with multi-stage builds
- **Clean Architecture**: Separation of concerns with repository pattern

### Documentation
- Complete developer documentation with getting started guides
- Architecture documentation with system overview and design decisions
- API documentation with endpoint specifications and examples
- Deployment guides for development, staging, and production environments
- Testing documentation with strategies and best practices

---

## Version History

- **v1.0.0**: Initial release with core functionality
- **Current**: Ongoing development with enhanced features and documentation

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for information on how to contribute to this project.

## License

This project is proprietary software owned by Adam Zaplatilek. See the main README.md for usage guidelines.
