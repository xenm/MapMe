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

### Common Log Patterns

#### Successful Authentication Flow
```
[INF] JWT token generated successfully. UserId: user-123, Username: john@example.com, TokenId: abc-def-123, ExpiresAt: 2025-08-27T20:57:34Z, RememberMe: false, Duration: 45.23ms
[DBG] JWT authentication successful. UserId: user-123, Username: john@example.com, TokenId: abc-def-123, Path: /api/profile, Method: GET, ClientIP: 192.168.1.100, Duration: 12.45ms
```

#### Failed Authentication Flow
```
[INF] JWT token validation failed - token expired. TokenPreview: eyJhbGciOiJIUzI1NiIs..., Duration: 8.12ms
[INF] JWT token validation failed. TokenPreview: eyJhbGciOiJIUzI1NiIs..., Path: /api/profile, Method: GET, ClientIP: 192.168.1.100, UserAgent: Mozilla/5.0...
```

#### Security Incident Pattern
```
[WRN] JWT token validation failed - invalid token. TokenPreview: invalid-token-here..., Duration: 5.67ms, Error: SecurityTokenMalformedException, Message: IDX10223: Unable to decode the payload
[WRN] Empty Bearer token provided. Path: /api/sensitive, Method: POST, ClientIP: 192.168.1.200
[ERR] Unexpected error during JWT authentication. Path: /api/admin, Method: DELETE, ClientIP: 192.168.1.200, Duration: 15.34ms, Error: ArgumentException
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
