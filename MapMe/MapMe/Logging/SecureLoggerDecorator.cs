using Microsoft.AspNetCore.Http;
using MapMe.Utilities;
using System.Diagnostics;

namespace MapMe.Logging;

/// <summary>
/// Logger decorator that automatically sanitizes JWT tokens and user-provided values for secure logging.
/// Implements the decorator pattern to enhance existing ILogger instances with security features.
/// </summary>
public class SecureLoggerDecorator<T> : ILogger<T>
{
    private readonly ILogger<T> _innerLogger;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public SecureLoggerDecorator(ILogger<T> innerLogger, IHttpContextAccessor? httpContextAccessor = null)
    {
        _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
        _httpContextAccessor = httpContextAccessor;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _innerLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _innerLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        // Create enhanced state with sanitized JWT context
        var enhancedState = CreateEnhancedLogState(state);
        
        _innerLogger.Log(logLevel, eventId, enhancedState, exception, (enhancedState, ex) =>
        {
            var originalMessage = formatter(state, ex);
            var contextInfo = GetSanitizedJwtContext();
            
            return string.IsNullOrEmpty(contextInfo) 
                ? originalMessage 
                : $"{originalMessage} | Context: {contextInfo}";
        });
    }

    /// <summary>
    /// Creates enhanced log state with sanitized JWT and HTTP context information.
    /// </summary>
    private object CreateEnhancedLogState<TState>(TState originalState)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
            return originalState!;

        var request = httpContext.Request;
        var jwtContext = ExtractSanitizedJwtInfo(request);
        var httpInfo = SecureLogging.CreateSafeHttpContext(httpContext);
        
        // Create anonymous object with original state plus sanitized context
        return new
        {
            OriginalState = originalState,
            JWT = jwtContext,
            HTTP = httpInfo,
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            TraceId = Activity.Current?.TraceId.ToString() ?? "[no-trace]"
        };
    }

    /// <summary>
    /// Extracts and sanitizes JWT-related information from the HTTP request.
    /// </summary>
    private object ExtractSanitizedJwtInfo(HttpRequest request)
    {
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        var sessionHeader = request.Headers["X-Session-Id"].FirstOrDefault();
        var sessionCookie = request.Cookies["MapMe-Session"];

        return new
        {
            AuthorizationScheme = ExtractAuthScheme(authHeader),
            HasAuthToken = !string.IsNullOrEmpty(authHeader) && authHeader.Contains(' '),
            TokenPreview = SecureLogging.ToTokenPreview(ExtractTokenFromAuth(authHeader)),
            HasSessionHeader = !string.IsNullOrEmpty(sessionHeader),
            SessionHeaderPreview = SecureLogging.ToTokenPreview(sessionHeader),
            HasSessionCookie = !string.IsNullOrEmpty(sessionCookie),
            SessionCookiePreview = SecureLogging.ToTokenPreview(sessionCookie)
        };
    }

    /// <summary>
    /// Gets sanitized JWT context as a formatted string for log message enhancement.
    /// </summary>
    private string GetSanitizedJwtContext()
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null)
            return string.Empty;

        var request = httpContext.Request;
        var parts = new List<string>();

        // Add authorization info
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            var scheme = ExtractAuthScheme(authHeader);
            var hasToken = authHeader.Contains(' ');
            parts.Add($"Auth={scheme}{(hasToken ? ":present" : ":missing")}");
        }

        // Add session info
        var sessionHeader = request.Headers["X-Session-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionHeader))
        {
            parts.Add("SessionHeader:present");
        }

        var sessionCookie = request.Cookies["MapMe-Session"];
        if (!string.IsNullOrEmpty(sessionCookie))
        {
            parts.Add("SessionCookie:present");
        }

        // Add trace ID if available
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
        {
            parts.Add($"TraceId={traceId[..8]}...");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
    }

    /// <summary>
    /// Safely extracts the authentication scheme from Authorization header.
    /// </summary>
    private static string ExtractAuthScheme(string? authHeader)
    {
        if (string.IsNullOrEmpty(authHeader))
            return "[none]";

        var parts = authHeader.Split(' ', 2);
        return SecureLogging.SanitizeForLog(parts[0], maxLength: 20, placeholder: "[unknown]");
    }

    /// <summary>
    /// Safely extracts token from Authorization header without logging the actual token.
    /// </summary>
    private static string? ExtractTokenFromAuth(string? authHeader)
    {
        if (string.IsNullOrEmpty(authHeader))
            return null;

        var parts = authHeader.Split(' ', 2);
        return parts.Length == 2 ? parts[1] : null;
    }
}

/// <summary>
/// Extension methods for easy registration of secure logger decorator.
/// </summary>
public static class SecureLoggerExtensions
{
    /// <summary>
    /// Wraps an existing logger with the secure logger decorator.
    /// </summary>
    public static ILogger<T> WithSecureLogging<T>(this ILogger<T> logger, IHttpContextAccessor? httpContextAccessor = null)
    {
        return new SecureLoggerDecorator<T>(logger, httpContextAccessor);
    }

    /// <summary>
    /// Adds secure logging services to the DI container.
    /// </summary>
    public static IServiceCollection AddSecureLogging(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        
        // Decorate existing ILogger<T> registrations with secure logging
        services.Decorate(typeof(ILogger<>), typeof(SecureLoggerDecorator<>));
        
        return services;
    }
}
