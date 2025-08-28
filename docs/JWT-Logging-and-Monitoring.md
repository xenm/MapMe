# JWT Authentication Logging and Monitoring Guide

## Overview

This document provides comprehensive guidance on production-level logging, monitoring, and observability for the MapMe JWT authentication system. It follows .NET best practices for structured logging, distributed tracing, and security monitoring.

## Logging Architecture

### Structured Logging with Serilog

The JWT authentication system uses structured logging with the following components:
- **Serilog**: Primary logging framework with structured data support
- **OpenTelemetry**: Distributed tracing and metrics collection
- **Application Insights**: Cloud-based monitoring and analytics
- **Custom Log Enrichers**: Correlation IDs, user context, and security metadata

### Log Levels and Categories

#### JWT Service Logging
```
MapMe.Services.JwtService:
- Debug: Token validation details, extraction operations
- Information: Token generation, refresh operations, authentication events
- Warning: Missing claims, invalid tokens, security violations
- Error: Token generation failures, unexpected validation errors
- Critical: Service initialization failures, configuration errors
```

#### Authentication Handler Logging
```
MapMe.Authentication.JwtAuthenticationHandler:
- Debug: Request details, header validation, successful authentications
- Information: Authentication failures, token validation results
- Warning: Invalid authorization schemes, empty tokens, suspicious activity
- Error: Unexpected authentication errors, service failures
- Critical: Handler initialization failures, security breaches
```

## Production Logging Configuration

### appsettings.Production.json
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.ApplicationInsights"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.AspNetCore.Authentication": "Information",
        "MapMe.Services.JwtService": "Information",
        "MapMe.Authentication.JwtAuthenticationHandler": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "telemetryConfiguration": "TelemetryConfiguration",
          "restrictedToMinimumLevel": "Information"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithEnvironmentName",
      "WithCorrelationId",
      "WithUserContext"
    ],
    "Properties": {
      "Application": "MapMe",
      "Environment": "Production"
    }
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key-here"
  },
  "OpenTelemetry": {
    "ServiceName": "MapMe",
    "ServiceVersion": "1.0.0",
    "Jaeger": {
      "AgentHost": "localhost",
      "AgentPort": 6831
    }
  }
}
```

### Program.cs Configuration
```csharp
using Serilog;
using Serilog.Events;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("Application", "MapMe")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry
var activitySource = new ActivitySource("MapMe.Authentication");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("MapMe.Authentication")
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnableGrpcAspNetCoreSupport = true;
            })
            .AddHttpClientInstrumentation()
            .AddJaegerExporter();
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    });

// Register activity source
builder.Services.AddSingleton(activitySource);
```

## Custom Log Enrichers

### Correlation ID Enricher
```csharp
public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
    }
}
```

### User Context Enricher
```csharp
public class UserContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst("userId")?.Value;
            var username = httpContext.User.FindFirst("username")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
            }
            
            if (!string.IsNullOrEmpty(username))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Username", username));
            }
        }
        
        var clientIp = httpContext?.Connection?.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientIP", clientIp));
        }
    }
}
```

## Security Monitoring and Alerting

### Key Security Metrics

#### Authentication Metrics
- **Token Generation Rate**: Monitor for unusual spikes
- **Token Validation Failures**: Track invalid/expired tokens
- **Authentication Failures**: Monitor failed login attempts
- **Token Refresh Patterns**: Analyze token usage patterns

#### Security Alerts
- **Brute Force Detection**: Multiple failed logins from same IP
- **Token Replay Attacks**: Same token used from multiple IPs
- **Unusual Token Patterns**: Tokens with unexpected claims
- **High Failure Rates**: Sudden increase in authentication failures

### Application Insights Queries

#### Failed Authentication Attempts
```kusto
traces
| where timestamp > ago(1h)
| where customDimensions.CategoryName == "MapMe.Authentication.JwtAuthenticationHandler"
| where message contains "validation failed"
| summarize count() by bin(timestamp, 5m), tostring(customDimensions.ClientIP)
| order by timestamp desc
```

#### Token Generation Patterns
```kusto
traces
| where timestamp > ago(24h)
| where customDimensions.CategoryName == "MapMe.Services.JwtService"
| where message contains "token generated successfully"
| summarize count() by bin(timestamp, 1h), tostring(customDimensions.UserId)
| order by timestamp desc
```

#### Security Anomalies
```kusto
traces
| where timestamp > ago(1h)
| where customDimensions.CategoryName startswith "MapMe"
| where severityLevel >= 2 // Warning and above
| where message contains any("invalid", "failed", "error", "unauthorized")
| summarize count() by bin(timestamp, 5m), tostring(customDimensions.ClientIP), message
| where count_ > 10 // Threshold for anomaly detection
```

## Performance Monitoring

### Key Performance Indicators

#### JWT Service Performance
- **Token Generation Time**: Target < 50ms
- **Token Validation Time**: Target < 10ms
- **Token Refresh Time**: Target < 100ms
- **Memory Usage**: Monitor for memory leaks

#### Authentication Handler Performance
- **Authentication Time**: Target < 20ms
- **Request Processing Time**: End-to-end authentication
- **Throughput**: Requests per second
- **Error Rate**: Authentication failures per minute

### Custom Metrics Collection

```csharp
public class JwtMetrics
{
    private readonly IMetrics _metrics;
    private readonly Counter<int> _tokenGenerationCounter;
    private readonly Counter<int> _tokenValidationCounter;
    private readonly Histogram<double> _tokenGenerationDuration;
    private readonly Histogram<double> _tokenValidationDuration;

    public JwtMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("MapMe.Authentication");
        
        _tokenGenerationCounter = meter.CreateCounter<int>(
            "jwt_tokens_generated_total",
            description: "Total number of JWT tokens generated");
            
        _tokenValidationCounter = meter.CreateCounter<int>(
            "jwt_tokens_validated_total", 
            description: "Total number of JWT tokens validated");
            
        _tokenGenerationDuration = meter.CreateHistogram<double>(
            "jwt_token_generation_duration_ms",
            description: "Duration of JWT token generation in milliseconds");
            
        _tokenValidationDuration = meter.CreateHistogram<double>(
            "jwt_token_validation_duration_ms",
            description: "Duration of JWT token validation in milliseconds");
    }

    public void RecordTokenGeneration(double durationMs, bool success, string userId)
    {
        _tokenGenerationCounter.Add(1, new KeyValuePair<string, object?>("success", success));
        _tokenGenerationDuration.Record(durationMs, new KeyValuePair<string, object?>("user_id", userId));
    }

    public void RecordTokenValidation(double durationMs, bool success, string? result)
    {
        _tokenValidationCounter.Add(1, new KeyValuePair<string, object?>("success", success));
        _tokenValidationDuration.Record(durationMs, new KeyValuePair<string, object?>("result", result ?? "unknown"));
    }
}
```

## Log Analysis and Troubleshooting

### Strongly-Typed Secure Logging Policy (Mandatory)

MapMe implements a **strongly-typed UserContext approach** to completely eliminate log forging vulnerabilities (CWE-117). This architecture replaces all string sanitization with type-safe logging that uses only GUIDs, enums, and hashed values.

#### Core Security Principles

- **Zero User-Provided Strings**: No raw user input is ever logged directly
- **Strongly-Typed Context**: All logging uses `UserContext` with GUIDs, enums, and safe values only
- **Structured Security Events**: `ISecureLoggingService.LogSecurityEvent()` with predefined event types
- **Safe Log Enrichment**: `UserContext.ToLogContext()` returns anonymous objects with hashed/safe fields only
- **Correlation IDs**: JWT tokens referenced by safe correlation IDs, never full token content

#### UserContext Architecture

```csharp
public record UserContext
{
    public Guid UserId { get; init; }
    public string UserIdHash { get; init; } // SHA256 hash for correlation
    public string UsernameHash { get; init; } // SHA256 hash, never raw username
    public string EmailHash { get; init; } // SHA256 hash, never raw email
    public bool IsEmailVerified { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    
    // Factory methods for safe context creation
    public static UserContext FromClaims(ClaimsPrincipal principal);
    public static UserContext CreateAnonymous();
    
    // Safe logging context - only hashes, GUIDs, and enums
    public object ToLogContext();
}
```

#### Secure Logging Service Implementation

```csharp
public interface ISecureLoggingService
{
    void LogSecurityEvent(SecurityEventType eventType, UserContext? userContext = null, object? additionalContext = null);
    object CreateSecureLogContext(object contextData);
}

public enum SecurityEventType
{
    UserLogin,
    UserLogout,
    TokenGenerated,
    TokenValidated,
    TokenExpired,
    TokenInvalid,
    AuthenticationFailed,
    AuthorizationDenied
}
```

#### Implementation Example (JwtService with UserContext)

```csharp
// Create strongly-typed user context from JWT claims
var userContext = UserContext.FromClaims(principal);

// Log security events with type-safe context - no user strings
_secureLoggingService.LogSecurityEvent(
    SecurityEventType.TokenValidated, 
    userContext,
    new { 
        TokenId = tokenId, // Safe GUID
        ExpiresAt = expiresAt, // Safe timestamp
        Duration = duration.TotalMilliseconds // Safe numeric value
    });

// Error logging with safe correlation ID only
_secureLoggingService.LogSecurityEvent(
    SecurityEventType.TokenInvalid,
    userContext,
    new {
        CorrelationId = Guid.NewGuid(), // Safe correlation ID
        ErrorType = ex.GetType().Name, // Safe exception type
        Duration = duration.TotalMilliseconds
    });
```

#### Benefits of UserContext Approach

- **Complete Log Injection Prevention**: No user input can manipulate log structure
- **GDPR Compliance**: Only hashed values logged, no PII exposure
- **Audit Trail Integrity**: Structured events prevent log tampering
- **Performance Optimized**: Efficient context creation and serialization
- **CodeQL Compliant**: Eliminates all detected log forging vulnerabilities

References: OWASP Log Injection Prevention, CWE-117 (Log Injection), NIST Cybersecurity Framework.

### Common Log Patterns with UserContext

#### Successful Authentication Flow
```json
[INF] Security event logged
{
  "EventType": "TokenGenerated",
  "UserContext": {
    "UserId": "550e8400-e29b-41d4-a716-446655440000",
    "UserIdHash": "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3",
    "UsernameHash": "b109f3bbbc244eb82441917ed06d618b9008dd09b3befd1b5e07394c706a8bb9",
    "IsActive": true,
    "LastLoginAt": "2025-01-27T20:57:34Z"
  },
  "AdditionalContext": {
    "TokenId": "abc-def-123",
    "ExpiresAt": "2025-08-27T20:57:34Z",
    "RememberMe": false,
    "Duration": 45.23
  }
}

[INF] Security event logged
{
  "EventType": "TokenValidated",
  "UserContext": {
    "UserId": "550e8400-e29b-41d4-a716-446655440000",
    "UserIdHash": "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3"
  },
  "AdditionalContext": {
    "TokenId": "abc-def-123",
    "RequestPath": "/api/profile",
    "RequestMethod": "GET",
    "Duration": 12.45
  }
}
```

#### Failed Authentication Flow
```json
[WRN] Security event logged
{
  "EventType": "TokenExpired",
  "UserContext": null,
  "AdditionalContext": {
    "CorrelationId": "123e4567-e89b-12d3-a456-426614174000",
    "Duration": 8.12
  }
}

[WRN] Security event logged
{
  "EventType": "TokenInvalid",
  "UserContext": null,
  "AdditionalContext": {
    "CorrelationId": "456e7890-e12b-34c5-d678-901234567890",
    "RequestPath": "/api/profile",
    "RequestMethod": "GET",
    "Duration": 5.67
  }
}
```

#### Security Incident Pattern
```json
[WRN] Security event logged
{
  "EventType": "AuthenticationFailed",
  "UserContext": null,
  "AdditionalContext": {
    "CorrelationId": "789e0123-e45f-67g8-h901-234567890123",
    "ErrorType": "SecurityTokenMalformedException",
    "ErrorMessage": "IDX10223: Unable to decode the payload",
    "Duration": 5.67
  }
}

[WRN] Security event logged
{
  "EventType": "AuthenticationFailed",
  "UserContext": null,
  "AdditionalContext": {
    "CorrelationId": "012e3456-e78f-90g1-h234-567890123456",
    "RequestPath": "/api/sensitive",
    "RequestMethod": "POST",
    "FailureReason": "EmptyBearerToken"
  }
}

[ERR] Security event logged
{
  "EventType": "AuthenticationFailed",
  "UserContext": null,
  "AdditionalContext": {
    "CorrelationId": "345e6789-e01f-23g4-h567-890123456789",
    "RequestPath": "/api/admin",
    "RequestMethod": "DELETE",
    "ErrorType": "ArgumentException",
    "Duration": 15.34
  }
}
```

### Do/Don't Quick Reference for UserContext Logging

#### ✅ DO - Use Strongly-Typed UserContext
- **Do** use `UserContext.FromClaims()` to create safe logging context
- **Do** log `UserContext.UserId` (GUID), `UserIdHash`, `UsernameHash`, `EmailHash`
- **Do** use `ISecureLoggingService.LogSecurityEvent()` with predefined event types
- **Do** log safe metadata: `TokenId` (GUID), `ExpiresAt`, durations, boolean flags
- **Do** use correlation IDs for tracking without exposing sensitive data
- **Do** log structured events with `SecurityEventType` enums

#### ❌ DON'T - Log Raw User Data
- **Don't** log raw usernames, emails, or any user-provided strings
- **Don't** log full JWT tokens or Authorization headers
- **Don't** use string concatenation with user input in log messages
- **Don't** log raw request paths, user agents, or client IPs without sanitization
- **Don't** bypass `ISecureLoggingService` for security-related logging

#### 🔒 Security Event Types to Use
```csharp
SecurityEventType.UserLogin          // User authentication success
SecurityEventType.TokenGenerated     // JWT token creation
SecurityEventType.TokenValidated     // JWT token validation success
SecurityEventType.TokenExpired       // JWT token expiration
SecurityEventType.TokenInvalid       // JWT token validation failure
SecurityEventType.AuthenticationFailed // Authentication errors
SecurityEventType.AuthorizationDenied  // Authorization failures
```

### Troubleshooting Guide

#### High Token Validation Failures
1. Check token expiration patterns
2. Verify secret key configuration
3. Monitor for token replay attacks
4. Validate client-side token storage

#### Performance Issues
1. Monitor token generation/validation times
2. Check for memory leaks in JWT service
3. Analyze request patterns and load
4. Verify database performance (if applicable)

#### Security Alerts
1. Investigate unusual IP patterns
2. Check for brute force attempts
3. Monitor token usage anomalies
4. Verify user behavior patterns

## Operational Procedures

### Daily Monitoring Tasks
- [ ] Review authentication failure rates
- [ ] Check performance metrics dashboard
- [ ] Analyze security alerts and anomalies
- [ ] Verify log ingestion and storage

### Weekly Security Reviews
- [ ] Analyze authentication patterns
- [ ] Review failed login attempts
- [ ] Check for new security threats
- [ ] Update security monitoring rules

### Monthly Performance Reviews
- [ ] Analyze authentication performance trends
- [ ] Review capacity planning metrics
- [ ] Optimize logging configuration
- [ ] Update alerting thresholds

## Emergency Response

### Security Incident Response
1. **Immediate Actions**
   - Identify affected users and tokens
   - Implement emergency token revocation
   - Block suspicious IP addresses
   - Escalate to security team

2. **Investigation**
   - Collect relevant logs and traces
   - Analyze attack patterns
   - Determine scope of compromise
   - Document findings

3. **Recovery**
   - Force password resets if needed
   - Update security configurations
   - Implement additional monitoring
   - Communicate with stakeholders

### Performance Incident Response
1. **Immediate Actions**
   - Check system resources and load
   - Identify performance bottlenecks
   - Implement temporary mitigations
   - Scale resources if needed

2. **Analysis**
   - Review performance metrics
   - Analyze slow query logs
   - Check for memory leaks
   - Identify root cause

3. **Resolution**
   - Implement performance fixes
   - Update monitoring thresholds
   - Optimize configurations
   - Document lessons learned

This comprehensive logging and monitoring setup ensures production-ready observability for the JWT authentication system with proper security monitoring, performance tracking, and incident response capabilities.
