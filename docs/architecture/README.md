# Architecture Documentation

This section contains comprehensive documentation about MapMe's system architecture, design decisions, and technical implementation.

## Quick Navigation

| Document | Purpose |
|----------|---------|
| [System Overview](./system-overview.md) | High-level system architecture and components |
| [Data Flow](./data-flow.md) | How data moves through the system |
| [Service Architecture](./service-architecture.md) | Microservices and component structure |
| [Database Design](./database-design.md) | Schema, relationships, and data modeling |
| [Security Architecture](./security-architecture.md) | Security design principles and implementation |
| [Technology Decisions](./technology-decisions.md) | Architecture Decision Records (ADRs) |
| [Scalability](./scalability.md) | Scaling strategies and performance considerations |

## Architecture Overview

MapMe is a modern dating application built with:

- **Frontend**: Blazor WebAssembly + Interactive SSR
- **Backend**: ASP.NET Core (.NET 10)
- **Database**: Azure Cosmos DB with in-memory fallback
- **Maps**: Google Maps JavaScript API with custom overlays
- **Authentication**: JWT with session management
- **Serialization**: System.Text.Json exclusively

## Key Architectural Principles

1. **Clean Architecture**: Clear separation of concerns with repository pattern
2. **API-First Design**: RESTful APIs with comprehensive documentation
3. **Security by Design**: JWT authentication, secure logging, input validation
4. **Performance Optimized**: Efficient data access patterns and caching
5. **Scalable**: Designed for horizontal scaling with stateless services

## Visual Documentation

See the [diagrams/](./diagrams/) folder for:
- System architecture diagrams
- Data flow visualizations
- Component interaction maps
- Database schema diagrams

## Related Documentation

- [Backend Documentation](../backend/README.md) - Implementation details
- [Frontend Documentation](../frontend/README.md) - Client architecture
- [API Documentation](../api/README.md) - API design and endpoints
- [Security Documentation](../security/README.md) - Security implementation

---

**Last Updated**: 2025-08-30  
**Maintained By**: Development Team
