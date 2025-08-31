# Frontend Documentation

This section contains comprehensive documentation for MapMe's Blazor WebAssembly frontend, including components, state management, and UI patterns.

## Quick Navigation

| Document | Purpose |
|----------|----------|
| [Getting Started](./getting-started.md) | Frontend-specific setup and development |
| [Project Structure](./project-structure.md) | Client app architecture and organization |
| [State Management](./state-management.md) | Client-side state and data management |
| [Navigation](./navigation.md) | Routing and navigation patterns |
| [UI Components](./ui-components/README.md) | Reusable component library |
| [Styling Guide](./styling-guide.md) | Design system and CSS conventions |
| [JavaScript Interop](./javascript-interop.md) | Blazor-JavaScript integration |
| [Performance](./performance.md) | Frontend optimization techniques |
| [Testing](./testing.md) | Frontend testing strategies |

## Technology Stack

### Core Technologies
- **Blazor WebAssembly**: Client-side C# execution
- **Interactive SSR**: Server-side rendering for performance
- **Bootstrap 5**: Responsive UI framework
- **System.Text.Json**: JSON serialization
- **SignalR**: Real-time communication (future)

### JavaScript Integration
- **Google Maps API**: Interactive mapping functionality
- **Custom Interop**: Blazor-JavaScript communication
- **ES6 Modules**: Modern JavaScript patterns
- **Web APIs**: Browser API integration

## Project Structure

```
MapMe.Client/
├── Pages/                      # Blazor pages/routes
│   ├── Map.razor              # Main map interface
│   ├── Profile.razor          # User profile management
│   ├── User.razor             # Public user profiles
│   ├── Chat.razor             # Messaging interface
│   ├── Login.razor            # Authentication
│   └── SignUp.razor           # User registration
├── Components/                 # Reusable UI components
│   ├── Layout/                # Layout components
│   ├── Forms/                 # Form components
│   └── Shared/                # Shared utilities
├── Services/                   # Client-side services
│   ├── UserProfileService.cs  # Profile management
│   ├── AuthenticationService.cs # Auth handling
│   └── ChatService.cs         # Chat functionality
├── Models/                     # Client-side data models
├── DTOs/                       # Data transfer objects
└── wwwroot/                    # Static assets
    ├── js/                     # JavaScript files
    ├── css/                    # Stylesheets
    └── images/                 # Image assets
```

## Key Features

### Interactive Map Interface
- **Google Maps Integration**: Full-featured mapping with custom overlays
- **Date Mark Creation**: Click-to-create location markers
- **Place Search**: Google Places API integration
- **Real-time Updates**: Dynamic marker and popup management

### User Profile System
- **Comprehensive Profiles**: Tinder-style dating app fields
- **Photo Management**: Upload, organize, and display photos
- **Activity Statistics**: Real-time metrics and achievements
- **Privacy Controls**: Granular visibility settings

### Real-time Chat
- **Messaging Interface**: Modern chat UI with conversations
- **Message Types**: Text, images, and location sharing
- **Read Status**: Message read/unread tracking
- **Conversation Management**: Archive and delete functionality

## State Management

### Client-Side Services
- **UserProfileService**: Profile and DateMark management
- **AuthenticationService**: JWT token handling and user state
- **ChatService**: Message and conversation management
- **LocationService**: Geographic operations

### Data Persistence
- **localStorage**: Client-side data caching
- **Session Storage**: Temporary state management
- **API Integration**: Server synchronization
- **Offline Support**: Basic offline functionality

## UI/UX Patterns

### Responsive Design
- **Mobile-First**: Optimized for mobile devices
- **Bootstrap Grid**: Responsive layout system
- **Touch-Friendly**: Mobile gesture support
- **Progressive Enhancement**: Works without JavaScript

### Component Architecture
- **Reusable Components**: Modular UI building blocks
- **Parameter Binding**: Flexible component configuration
- **Event Handling**: Clean event propagation
- **Lifecycle Management**: Proper component lifecycle

## Related Documentation

- [Architecture Overview](../architecture/system-overview.md) - System design
- [Backend Integration](../backend/README.md) - API integration
- [Testing Frontend](../testing/frontend-testing.md) - Testing strategies
- [Deployment](../deployment/frontend-deployment.md) - Deployment procedures

---

**Last Updated**: 2025-08-30  
**Maintained By**: Frontend Development Team  
**Review Schedule**: Monthly
