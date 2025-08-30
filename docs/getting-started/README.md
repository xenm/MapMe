# Getting Started with MapMe

Welcome to MapMe! This section will help you get up and running quickly, whether you're a new team member or an external developer.

## Quick Navigation

| Document | Purpose | Time Required |
|----------|---------|---------------|
| [Quick Start](./quick-start.md) | Get the app running in 5 minutes | 5 minutes |
| [Prerequisites](./prerequisites.md) | Required tools and dependencies | 10 minutes |
| [Local Development](./local-development.md) | Complete local setup guide | 30 minutes |
| [First Contribution](./first-contribution.md) | Make your first code contribution | 45 minutes |

## What is MapMe?

MapMe is a modern dating application built with Blazor WebAssembly and .NET 10, featuring:

- **Interactive Maps**: Google Maps integration for location-based dating
- **Date Marks**: Save and share memorable date locations
- **User Profiles**: Comprehensive Tinder-style profiles
- **Real-time Chat**: Messaging system for user communication
- **Activity Statistics**: Track and display user engagement metrics

## Architecture Overview

```
MapMe/
├── MapMe/              # Server project (ASP.NET Core)
├── MapMe.Client/       # Client project (Blazor WebAssembly)
└── MapMe.Tests/        # Unit and integration tests
```

**Technology Stack:**
- Frontend: Blazor WebAssembly + Interactive SSR
- Backend: ASP.NET Core (.NET 10)
- Database: Azure Cosmos DB (with in-memory fallback)
- Maps: Google Maps JavaScript API
- Authentication: JWT with session management

## Next Steps

1. **New to the project?** Start with [Quick Start](./quick-start.md)
2. **Setting up development?** Follow [Local Development](./local-development.md)
3. **Ready to contribute?** Read [First Contribution](./first-contribution.md)
4. **Need help?** Check [Troubleshooting](../troubleshooting/README.md)

## Support

- **Documentation Issues**: Create an issue with `documentation` label
- **Setup Problems**: Check [Troubleshooting](../troubleshooting/README.md)
- **Questions**: Ask in team chat or create a discussion

---

**Last Updated**: 2025-08-30  
**Next Review**: 2025-09-30
