# Navigation System

## Overview
MapMe implements a comprehensive navigation system with authentication-aware routing, responsive design, and context-aware navigation patterns for the dating app experience.

## Architecture

### MainLayout.razor
The primary navigation component featuring:
- **Bootstrap Navbar**: Fixed-top responsive navigation with primary branding
- **Authentication-Aware**: Different navigation for authenticated vs public users
- **Smart Page Detection**: Automatic routing based on authentication state
- **Active Page Highlighting**: Visual indication of current page

### Public vs Protected Pages
```csharp
// Public pages (no authentication required)
- "/" (root/home)
- "/login" 
- "/signup"
- "/register"
- "/forgot-password"
- "/reset-password"

// Protected pages (authentication required)
- "/map" (primary app experience)
- "/profile" (current user profile)
- "/user/{username}" (other user profiles)
- "/chat" (conversations hub)
```

### Navigation Components

#### Authenticated Navigation
- **Map**: Primary app experience with interactive maps and DateMark management
- **Profile**: Personal profile management and activity statistics
- **Chat**: Private messaging and conversation management
- **Logout**: Secure session termination with redirect

#### Public Navigation
- **Login**: User authentication with Google OAuth support
- **Sign Up**: New user registration with profile creation

## Navigation Patterns

### Query Parameter Context
- **Map Navigation**: `?lat={lat}&lng={lng}&zoom={zoom}` for specific locations
- **Chat Navigation**: `?to={username}` to open specific conversations
- **DateMark Editing**: `?edit={dateMarkId}` for edit intent
- **Popup Display**: `?showPopup=true&placeId={placeId}` for map popups

### Cross-Page State Management
- **UserProfileService**: Maintains user profile and DateMark data across pages
- **AuthenticationService**: Manages authentication state and user sessions
- **JavaScript Interop**: Preserves map state and user interactions

### Navigation Flows

#### Profile to Map Integration
```csharp
// "View on Map" from Profile DateMarks
Navigation.NavigateTo($"/map?lat={lat}&lng={lng}&zoom=15&showPopup=true&placeId={placeId}");
```

#### Authentication Redirects
```csharp
// Post-login navigation
Navigation.NavigateTo("/map", forceLoad: true);

// Unauthenticated access protection
Navigation.NavigateTo("/login");
```

## Responsive Design

### Mobile Navigation
- **Collapsible Navbar**: Bootstrap navbar-toggler for mobile screens
- **Touch-Friendly**: Optimized button sizes and spacing
- **Responsive Layout**: Adapts to different screen sizes

### Desktop Navigation
- **Fixed Navigation**: Always-visible top navigation bar
- **Hover Effects**: Visual feedback for navigation interactions
- **Keyboard Navigation**: Full keyboard accessibility support

## Security & Authentication

### Route Protection
- **AuthorizeView**: Blazor component for authentication-based rendering
- **Automatic Redirects**: Unauthenticated users redirected to login
- **Session Management**: JWT token-based authentication with refresh

### Navigation Security
- **CSRF Protection**: Secure form submissions and navigation
- **Input Validation**: Query parameter sanitization and validation
- **Authorization Checks**: User-specific content and navigation options

## Testing Coverage

### Integration Tests
- **Authentication Flows**: Login → Map navigation testing
- **Cross-Page Navigation**: Profile → Map → Chat workflows
- **Query Parameter Handling**: URL parameter parsing and navigation
- **Error Scenarios**: Invalid routes and authentication failures

### Manual Testing Scenarios
- **Deep Links**: Direct URL access with authentication checks
- **Refresh Behavior**: Page reload with state preservation
- **Mobile Navigation**: Touch interactions and responsive behavior
- **Browser Navigation**: Back/forward button handling

## Performance Optimization

### Navigation Performance
- **Lazy Loading**: Components loaded on-demand
- **State Caching**: User profile and DateMark data cached locally
- **Efficient Routing**: Minimal re-renders during navigation
- **JavaScript Optimization**: Map state preservation during navigation

## Future Enhancements

### Planned Features
- **Navigation History**: Back/forward state preservation with browser history API
- **Map Viewport Memory**: Remember last map location and zoom level
- **Breadcrumb Navigation**: Hierarchical navigation for complex workflows
- **Navigation Analytics**: User navigation pattern tracking

### Advanced Navigation
- **Deep Linking**: Enhanced URL structure for sharing specific app states
- **Progressive Navigation**: Guided user onboarding flows
- **Context Menus**: Right-click navigation options
- **Keyboard Shortcuts**: Power user navigation shortcuts

## Implementation Details

### Key Files
- `MainLayout.razor` - Primary navigation component
- `NavMenu.razor` - Legacy navigation (unused)
- `Map.razor` - Query parameter handling
- `Profile.razor` - Cross-page navigation integration

### Dependencies
- **Bootstrap 5.3.2**: Responsive navigation components
- **Blazor WebAssembly**: Client-side routing and navigation
- **JavaScript Interop**: Map state management during navigation
- **System.Text.Json**: Navigation state serialization

