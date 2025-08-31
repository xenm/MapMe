# MapMe External Dependencies

This directory contains documentation for all external dependencies and third-party integrations used in the MapMe dating application.

## 🔗 Core Dependencies

### [Google Maps](./google-maps.md)
Google Maps JavaScript API integration for interactive mapping, place search, and geolocation services. Essential for the core map functionality and DateMark visualization.

**Key Features:**
- Interactive map rendering and controls
- Place search and autocomplete
- Geolocation and geocoding services
- Custom markers and info windows

### [Google OAuth](./google-oauth.md)
Google Identity Services integration for secure user authentication and profile creation. Provides seamless login experience with Google accounts.

**Key Features:**
- OAuth 2.0 authentication flow
- User profile information retrieval
- Secure token management
- Account linking and registration

### [Azure Cosmos DB](./azure-cosmos.md)
NoSQL database service for production data storage with global distribution and automatic scaling capabilities.

**Key Features:**
- Document-based data storage
- Geospatial query capabilities
- System.Text.Json serialization
- Automatic scaling and global distribution

## 🏗️ Dependency Architecture

### Integration Layers
```
Frontend (Blazor WebAssembly)
├── Google Maps JavaScript API
├── Google Identity Services
└── HTTP Client (API calls)

Backend (ASP.NET Core)
├── Google OAuth validation
├── Cosmos DB SDK
└── System.Text.Json serialization
```

### Data Flow
1. **Authentication**: Google OAuth → JWT tokens → API authorization
2. **Maps**: JavaScript API → Blazor interop → DateMark services
3. **Storage**: Application data → Cosmos DB repositories → JSON serialization

## ⚙️ Configuration Requirements

### Development Environment
- **Google Cloud Console**: API keys and OAuth client configuration
- **Cosmos DB Emulator**: Local development database
- **User Secrets**: Secure configuration storage

### Production Environment
- **Google Cloud Project**: Production API keys and OAuth settings
- **Azure Cosmos DB**: Production database instance
- **Environment Variables**: Secure configuration management

## 🔐 Security Considerations

### API Key Management
- **Development**: User Secrets for local development
- **Production**: Environment variables and Azure Key Vault
- **Restrictions**: Domain and API restrictions for security

### Authentication Security
- **OAuth Tokens**: Secure token validation and refresh
- **JWT Signing**: HMAC SHA256 with secure secret keys
- **HTTPS Only**: All authentication flows over secure connections

### Database Security
- **Connection Strings**: Encrypted and secured
- **Access Controls**: Role-based access permissions
- **Data Encryption**: Encryption at rest and in transit

## 📊 Dependency Status

| Dependency | Version | Status | Configuration |
|------------|---------|--------|---------------|
| Google Maps API | Latest | ✅ Active | ✅ Configured |
| Google OAuth | Latest | ✅ Active | ✅ Configured |
| Azure Cosmos DB | Latest | ✅ Active | ✅ Configured |

## 🚀 Setup Order

### Recommended Configuration Sequence
1. **Google Cloud Console Setup**
   - Create project and enable APIs
   - Configure OAuth consent screen
   - Generate API keys and OAuth client

2. **Local Development**
   - Install Cosmos DB Emulator
   - Configure User Secrets
   - Test API connections

3. **Production Deployment**
   - Set up Azure Cosmos DB instance
   - Configure production OAuth settings
   - Deploy with environment variables

## 🔧 Development Tools

### Local Development
- **Cosmos DB Emulator**: Local database for development
- **PowerShell Scripts**: Automated setup and management
- **Docker Compose**: Container-based development environment

### Monitoring & Debugging
- **Azure Portal**: Cosmos DB monitoring and metrics
- **Google Cloud Console**: API usage and quotas
- **Browser Dev Tools**: JavaScript API debugging

## 📈 Performance Considerations

### Google Maps Optimization
- **Lazy Loading**: Load maps only when needed
- **Marker Clustering**: Efficient rendering of multiple markers
- **API Quotas**: Monitor and optimize API usage

### Cosmos DB Optimization
- **Indexing Strategy**: Optimized indexes for query performance
- **Partition Keys**: Efficient data distribution
- **Request Units**: Cost-effective resource utilization

### Caching Strategy
- **Client-Side**: Local storage for user data
- **API Responses**: Cached responses where appropriate
- **Static Assets**: CDN for external resources

## 🛠️ Troubleshooting

### Common Issues
- **API Key Errors**: Verify keys and restrictions
- **OAuth Failures**: Check redirect URIs and consent screen
- **Database Connectivity**: Verify connection strings and permissions

### Debug Resources
- **Browser Console**: JavaScript API errors
- **Server Logs**: Backend integration issues
- **Azure Diagnostics**: Database performance and errors

## 🔄 Maintenance

### Regular Tasks
- **API Key Rotation**: Periodic security updates
- **Dependency Updates**: Keep SDKs and libraries current
- **Usage Monitoring**: Track API quotas and costs

### Security Reviews
- **Access Permissions**: Regular audit of API access
- **Configuration Validation**: Verify security settings
- **Compliance Checks**: Ensure regulatory compliance

