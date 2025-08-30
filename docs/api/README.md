# API Documentation

This section contains comprehensive API documentation for MapMe's REST endpoints, authentication, and integration guides.

## Quick Navigation

| Document | Purpose |
|----------|----------|
| [Authentication](./authentication.md) | API authentication methods and JWT handling |
| [Rate Limiting](./rate-limiting.md) | API rate limiting policies and quotas |
| [Versioning](./versioning.md) | API versioning strategy and compatibility |
| [Endpoints](./endpoints/README.md) | Complete API endpoint documentation |
| [Webhooks](./webhooks.md) | Webhook implementation and usage |
| [SDK Guides](./sdk-guides.md) | Integration examples and best practices |
| [OpenAPI Specification](./openapi.yaml) | Machine-readable API specification |

## API Overview

MapMe provides a comprehensive REST API for all application functionality:

### Base URL
- **Development**: `https://localhost:8008/api`
- **Production**: `https://mapme.app/api`

### Authentication
All protected endpoints require JWT authentication via the `Authorization` header:
```
Authorization: Bearer <jwt-token>
```

### Content Type
All requests and responses use JSON:
```
Content-Type: application/json
```

## Endpoint Categories

### Authentication Endpoints
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/google-login` - Google OAuth login
- `POST /api/auth/validate-token` - Token validation
- `POST /api/auth/refresh-token` - Token refresh

### User Profile Endpoints
- `GET /api/profiles/current` - Get current user profile
- `PUT /api/profiles/current` - Update current user profile
- `GET /api/profiles/{username}` - Get user profile by username
- `POST /api/profiles/photos` - Upload profile photo
- `DELETE /api/profiles/photos/{photoId}` - Delete profile photo

### DateMark Endpoints
- `GET /api/datemarks` - Get user's DateMarks
- `POST /api/datemarks` - Create new DateMark
- `PUT /api/datemarks/{id}` - Update DateMark
- `DELETE /api/datemarks/{id}` - Delete DateMark
- `GET /api/datemarks/nearby` - Find nearby DateMarks

### Chat Endpoints
- `GET /api/chat/conversations` - Get user conversations
- `POST /api/chat/messages` - Send message
- `GET /api/chat/conversations/{id}/messages` - Get conversation messages
- `POST /api/chat/messages/read` - Mark messages as read
- `POST /api/chat/conversations/archive` - Archive conversation

## Response Format

### Success Response
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully"
}
```

### Error Response
```json
{
  "success": false,
  "error": "Error description",
  "details": { ... },
  "timestamp": "2025-08-30T10:50:35Z"
}
```

## Rate Limiting

- **Authenticated Users**: 1000 requests per hour
- **Anonymous Users**: 100 requests per hour
- **Burst Limit**: 50 requests per minute

Rate limit headers included in all responses:
- `X-RateLimit-Limit`: Request limit per window
- `X-RateLimit-Remaining`: Remaining requests
- `X-RateLimit-Reset`: Window reset time

## Error Codes

| HTTP Status | Description |
|-------------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Authentication required |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource doesn't exist |
| 429 | Too Many Requests - Rate limit exceeded |
| 500 | Internal Server Error |

## Related Documentation

- [Backend API Implementation](../backend/README.md) - Server-side implementation
- [Frontend API Integration](../frontend/api-integration.md) - Client-side usage
- [Security](../security/authentication.md) - API security measures
- [Testing](../testing/api-testing.md) - API testing strategies

---

**Last Updated**: 2025-08-30  
**API Version**: v1.0  
**Maintained By**: Backend Development Team
