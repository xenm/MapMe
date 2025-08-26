using Serilog.Core;
using Serilog.Events;

namespace MapMe.Logging;

/// <summary>
/// Enriches log events with user context and request information
/// </summary>
public class UserContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    // Parameterless constructor for Serilog
    public UserContextEnricher() : this(null!)
    {
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Add user information if authenticated
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst("userId")?.Value;
            var username = httpContext.User.FindFirst("username")?.Value;
            var tokenId = httpContext.User.FindFirst("sessionId")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));
            }
            
            if (!string.IsNullOrEmpty(username))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Username", username));
            }
            
            if (!string.IsNullOrEmpty(tokenId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TokenId", tokenId));
            }
        }
        
        // Add request information
        var clientIp = httpContext.Connection?.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientIP", clientIp));
        }
        
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        if (!string.IsNullOrEmpty(userAgent))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserAgent", userAgent));
        }
        
        var requestPath = httpContext.Request.Path.Value;
        if (!string.IsNullOrEmpty(requestPath))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", requestPath));
        }
        
        var requestMethod = httpContext.Request.Method;
        if (!string.IsNullOrEmpty(requestMethod))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestMethod", requestMethod));
        }
        
        // Add request ID
        var requestId = httpContext.TraceIdentifier;
        if (!string.IsNullOrEmpty(requestId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestId", requestId));
        }
    }
}
