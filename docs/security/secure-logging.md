# Secure Logging

## Secure Logging Policy

**MANDATORY POLICY:** Never log sensitive information in production or development environments.

### What NOT to Log

**Strictly Prohibited:**
- Raw JWT tokens or Authorization headers
- Raw email addresses
- Passwords or password hashes
- Session tokens or API keys
- Personal identifiable information (PII)
- Raw user input without sanitization

### What TO Log

**Approved for Logging:**
- Sanitized user identifiers
- Token previews (first/last 4 characters)
- Request metadata (method, path, status code)
- Performance metrics
- Error categories and sanitized error messages
- Authentication success/failure events (without credentials)

## Sanitization Helpers

### Available Utilities

**`SecureLogging.SanitizeForLog(string? value, int maxLength = 200, string placeholder = "[empty]")`**
- Removes control characters (newlines, carriage returns, tabs)
- Strips HTML tags to prevent injection attacks
- Enforces maximum length limits
- Normalizes whitespace
- Prevents log forging and parsing tool breakage

**`SecureLogging.SanitizeUserIdForLog(string? userId)`**
- Sanitizes user identifiers with alphanumeric validation
- Maintains debugging capability while protecting privacy
- Safe for structured logging

**`SecureLogging.SanitizeHeaderForLog(string? headerValue, string headerName = "header")`**
- Safely logs HTTP headers with Authorization header special handling
- Masks sensitive tokens and API keys
- Preserves debugging information without security risks

**`SecureLogging.ToTokenPreview(string? token, int previewLength = 20)`**
- Creates non-sensitive JWT token previews
- Shows only first 20 characters for debugging
- Never logs complete tokens or sensitive claims

**`SecureLogging.SanitizeEmailForLog(string? email)`**
- Email sanitization with format validation
- Protects PII while maintaining debugging capability

**`SecureLogging.SanitizePathForLog(string? path)`**
- Request path sanitization for safe logging
- Removes potentially malicious path components

**`SecureLogging.CreateSafeHttpContext(HttpContext? httpContext)`**
- HTTP context sanitization for structured logging
- Extracts safe debugging information without exposing sensitive data

### Usage Examples

**DO - Secure Logging:**
```csharp
// Good: Sanitized logging
_logger.LogInformation("User {UserId} authenticated successfully", 
    SecureLogging.SanitizeUserIdForLog(userId));

// Good: Token preview for debugging
_logger.LogDebug("Token validation successful for {TokenPreview}", 
    token.ToTokenPreview());

// Good: Sanitized error context
_logger.LogError("Authentication failed: {Error}", 
    SecureLogging.SanitizeForLog(errorMessage));
```

**DON'T - Insecure Logging:**
```csharp
// BAD: Raw token logging
_logger.LogDebug("JWT token: {Token}", jwtToken);

// BAD: Raw email logging
_logger.LogInformation("User email: {Email}", user.Email);

// BAD: Raw user input
_logger.LogError("Invalid input: {Input}", userInput);
```

## Implementation Highlights

### Decorator Pattern
- Enriched, sanitized logging where needed
- Serilog request logging augmented with safe context helpers
- Applies to auth flows (JWT/session), headers, paths, and identifiers

### Structured Logging
- Use structured logging with minimal, sanitized context
- Consistent log message formats
- Proper log levels for different scenarios

### Authentication Flow Logging
```csharp
// Login attempt
_logger.LogInformation("Login attempt for user {UserIdPreview}", 
    SecureLogging.SanitizeUserIdForLog(userId));

// JWT generation
_logger.LogDebug("JWT generated for user {UserIdPreview}, expires {ExpiresAt}", 
    SecureLogging.SanitizeUserIdForLog(userId), expiresAt);

// Token validation
_logger.LogDebug("Token validation successful for {TokenPreview}", 
    token.ToTokenPreview());
```

## Testing and Validation

### Automated Tests
- Automated tests cover sanitization behavior
- Verified no regressions after security fixes
- Test cases for log injection prevention
- Validation of token preview functionality

### Security Scanning
- CodeQL log-forging findings resolved
- Regular security scans for logging vulnerabilities
- Continuous monitoring for sensitive data exposure

## Production Monitoring

### Log Analysis
- Monitor for authentication patterns
- Track error rates and types
- Alert on suspicious authentication activities
- Performance monitoring through sanitized logs

### Compliance
- Ensure GDPR/privacy compliance in logging
- Regular audit of logged data
- Data retention policies for logs
- Secure log storage and access controls

## Best Practices

### Development
- Always use sanitization helpers
- Review logging statements in code reviews
- Test logging output in development
- Use structured logging consistently

### Production
- Configure appropriate log levels
- Implement log rotation and retention
- Secure log storage and transmission
- Regular security audits of logging practices

### Incident Response
- Sanitized logs aid in debugging without exposing sensitive data
- Token previews enable correlation without security risks
- Structured logs facilitate automated analysis

---

**Related Documentation:**
- [Security Overview](README.md)
- [Authentication](authentication.md)
- [Data Protection](data-protection.md)
- [Operations Monitoring](../operations/monitoring.md)
