# System Overview

MapMe is a modern Blazor dating application with Google Maps integration, featuring comprehensive user profiles, location-based date marking, and social discovery features.

## High-Level Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Client App    │    │   Server API    │    │    Database     │
│  (Blazor WASM)  │◄──►│  (.NET Core)    │◄──►│  (Cosmos DB)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Google Maps    │    │   External      │    │   File Storage  │
│     API         │    │   Services      │    │   (Local/Cloud) │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Core Components

### Frontend (Blazor WebAssembly)
- **Interactive UI**: Blazor components with Bootstrap styling
- **Map Integration**: Google Maps JavaScript API with custom overlays
- **State Management**: Client-side services with localStorage persistence
- **Real-time Updates**: SignalR integration for chat functionality

### Backend (ASP.NET Core)
- **API Layer**: RESTful endpoints for all business operations
- **Business Logic**: Services for user management, dating features, and chat
- **Data Access**: Repository pattern with Cosmos DB and in-memory implementations
- **Authentication**: JWT-based authentication with session management

### Database Layer
- **Primary**: Azure Cosmos DB for production scalability
- **Development**: In-memory repositories for fast development and testing
- **Caching**: Redis integration for session and frequently accessed data

## Technology Stack

### Frontend Technologies
- **Blazor WebAssembly**: Client-side C# execution
- **Interactive SSR**: Server-side rendering for initial load performance
- **Bootstrap 5**: Responsive UI framework
- **Google Maps API**: Interactive mapping and location services
- **System.Text.Json**: JSON serialization

### Backend Technologies
- **.NET 10**: Latest .NET framework with preview features
- **ASP.NET Core**: Web API and hosting framework
- **Entity Framework**: Data access abstraction (future enhancement)
- **JWT Authentication**: Secure token-based authentication
- **Serilog**: Structured logging with secure practices

### Data & Storage
- **Azure Cosmos DB**: NoSQL database for production
- **In-Memory Repositories**: Development and testing
- **localStorage**: Client-side data persistence
- **Azure Blob Storage**: File and image storage (future)

### External Services
- **Google Maps API**: Maps, Places, and Geocoding
- **Google OAuth**: Social authentication
- **SendGrid**: Email services (future)
- **Azure Application Insights**: Monitoring and analytics (future)

## Project Structure

```
MapMe/
├── MapMe/                          # Server project
│   ├── Controllers/                # API controllers
│   ├── Services/                   # Business logic services
│   ├── Repositories/               # Data access layer
│   ├── Models/                     # Server-side data models
│   ├── Authentication/             # JWT and auth services
│   └── Program.cs                  # Server configuration
├── MapMe.Client/                   # Client project
│   ├── Pages/                      # Blazor pages
│   ├── Components/                 # Reusable UI components
│   ├── Services/                   # Client-side services
│   ├── Models/                     # Client-side models
│   └── wwwroot/js/                 # JavaScript interop
└── MapMe.Tests/                    # Test projects
    ├── Unit/                       # Unit tests
    ├── Integration/                # Integration tests
    └── TestUtilities/              # Test helpers
```

## Runtime Modes

### Development Mode
- **Interactive SSR**: Server-side rendering for fast development
- **Hot Reload**: Real-time code changes without restart
- **In-Memory Data**: Fast development without external dependencies
- **Detailed Logging**: Comprehensive debugging information

### Production Mode
- **WebAssembly**: Client-side execution for better performance
- **Cosmos DB**: Scalable NoSQL database
- **CDN Integration**: Static asset delivery
- **Optimized Logging**: Secure, performance-focused logging

## Data Flow

### User Authentication Flow
1. User submits credentials via login form
2. Server validates credentials and generates JWT token
3. Client stores token and includes in subsequent API requests
4. Server validates token on each protected endpoint access

### Date Mark Creation Flow
1. User clicks location on Google Maps
2. JavaScript interop captures coordinates and place details
3. Blazor component displays creation form
4. Client service validates and submits to API
5. Server processes and stores in database
6. Client updates local state and UI

### Profile Management Flow
1. User navigates to profile page
2. Client service loads profile data from API
3. User edits profile information
4. Client validates changes locally
5. API processes updates and returns confirmation
6. Client updates local cache and UI

## Security Architecture

### Authentication & Authorization
- **JWT Tokens**: Stateless authentication with configurable expiration
- **Role-Based Access**: User roles and permissions system
- **Session Management**: Secure session handling with refresh tokens

### Data Protection
- **Input Validation**: Comprehensive validation on client and server
- **SQL Injection Prevention**: Parameterized queries and ORM usage
- **XSS Protection**: Content Security Policy and input sanitization
- **HTTPS Enforcement**: All communication encrypted in transit

### Privacy Controls
- **Profile Visibility**: Granular privacy settings for user profiles
- **Data Minimization**: Only collect necessary user information
- **Secure Logging**: No sensitive data in application logs
- **GDPR Compliance**: User data rights and deletion capabilities

## Performance Considerations

### Frontend Optimization
- **Lazy Loading**: Components loaded on demand
- **Image Optimization**: Responsive images with proper sizing
- **Caching Strategy**: Aggressive caching of static assets
- **Bundle Optimization**: Minimal JavaScript and CSS bundles

### Backend Optimization
- **Database Indexing**: Optimized queries with proper indexes
- **Connection Pooling**: Efficient database connection management
- **Response Caching**: Cache frequently accessed data
- **Async Operations**: Non-blocking I/O operations throughout

### Scalability Design
- **Stateless Services**: Horizontal scaling capability
- **Database Partitioning**: Cosmos DB partition key strategy
- **CDN Integration**: Global content delivery
- **Load Balancing**: Multiple server instance support

## Monitoring & Observability

### Application Monitoring
- **Health Checks**: Endpoint health monitoring
- **Performance Metrics**: Response time and throughput tracking
- **Error Tracking**: Comprehensive error logging and alerting
- **User Analytics**: Usage patterns and feature adoption

### Infrastructure Monitoring
- **Resource Utilization**: CPU, memory, and storage monitoring
- **Database Performance**: Query performance and optimization
- **Network Monitoring**: Latency and connectivity tracking
- **Security Monitoring**: Authentication and authorization events

## Related Documentation

- [Data Flow](./data-flow.md) - Detailed data flow diagrams
- [Service Architecture](./service-architecture.md) - Service design patterns
- [Database Design](./database-design.md) - Data modeling and schema
- [Security Architecture](./security-architecture.md) - Security implementation
- [Technology Decisions](./technology-decisions.md) - Architecture decisions

---

**Last Updated**: 2025-08-30  
**Next Review**: 2025-09-30  
**Maintained By**: Development Team
