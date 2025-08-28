using Microsoft.AspNetCore.Http;
using MapMe.Models;
using System.Security.Claims;

namespace MapMe.Services;

/// <summary>
/// Secure logging service that uses strongly-typed UserContext to eliminate log injection vulnerabilities.
/// This approach completely avoids logging user-provided strings, using only GUIDs, enums, and safe values.
/// </summary>
public interface ISecureLoggingService
{
    /// <summary>
    /// Gets the current user context for secure logging
    /// </summary>
    /// <returns>Strongly-typed UserContext safe for logging</returns>
    UserContext GetCurrentUserContext();

    /// <summary>
    /// Creates a secure log context with user information and request metadata
    /// </summary>
    /// <param name="additionalContext">Additional safe context properties</param>
    /// <returns>Structured logging context safe from injection attacks</returns>
    object CreateSecureLogContext(object? additionalContext = null);

    /// <summary>
    /// Logs a security event with safe user context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="level">Log level</param>
    /// <param name="eventType">Type of security event</param>
    /// <param name="message">Safe message template</param>
    /// <param name="additionalContext">Additional safe context</param>
    void LogSecurityEvent<T>(ILogger<T> logger, LogLevel level, SecurityEventType eventType, 
        string message, object? additionalContext = null);
}

/// <summary>
/// Implementation of secure logging service using UserContext
/// </summary>
public class SecureLoggingService : ISecureLoggingService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecureLoggingService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Gets the current user context for secure logging
    /// </summary>
    /// <returns>Strongly-typed UserContext safe for logging</returns>
    public UserContext GetCurrentUserContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return UserContext.FromClaims(httpContext?.User, httpContext);
    }

    /// <summary>
    /// Creates a secure log context with user information and request metadata
    /// </summary>
    /// <param name="additionalContext">Additional safe context properties</param>
    /// <returns>Structured logging context safe from injection attacks</returns>
    public object CreateSecureLogContext(object? additionalContext = null)
    {
        var userContext = GetCurrentUserContext();
        var httpContext = _httpContextAccessor.HttpContext;

        var baseContext = new
        {
            // User context - all safe values
            User = userContext.ToLogContext(),
            
            // Request context - only safe values
            Request = new
            {
                Method = httpContext?.Request.Method ?? "UNKNOWN",
                PathHash = ComputePathHash(httpContext?.Request.Path.Value),
                Scheme = httpContext?.Request.Scheme ?? "UNKNOWN",
                Protocol = httpContext?.Request.Protocol ?? "UNKNOWN",
                ContentLength = httpContext?.Request.ContentLength ?? 0,
                HasFormData = httpContext?.Request.HasFormContentType,
                QueryParameterCount = httpContext?.Request.Query.Count,
                HeaderCount = httpContext?.Request.Headers.Count,
                Timestamp = DateTime.UtcNow,
                TraceId = System.Diagnostics.Activity.Current?.Id ?? "none"
            },

            // Response context - safe values only
            Response = new
            {
                StatusCode = httpContext?.Response.StatusCode ?? 0,
                HasStarted = httpContext?.Response.HasStarted ?? false,
                ContentLength = httpContext?.Response.ContentLength ?? 0
            }
        };

        // Merge with additional context if provided
        if (additionalContext == null)
        {
            return baseContext;
        }

        // Create combined context
        return new
        {
            baseContext.User,
            baseContext.Request,
            baseContext.Response,
            Additional = additionalContext
        };
    }

    /// <summary>
    /// Logs a security event with safe user context
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="level">Log level</param>
    /// <param name="eventType">Type of security event</param>
    /// <param name="message">Safe message template</param>
    /// <param name="additionalContext">Additional safe context</param>
    public void LogSecurityEvent<T>(ILogger<T> logger, LogLevel level, SecurityEventType eventType, 
        string message, object? additionalContext = null)
    {
        var secureContext = CreateSecureLogContext(new
        {
            EventType = eventType.ToString(),
            EventId = Guid.NewGuid(),
            Additional = additionalContext
        });

        using (logger.BeginScope(secureContext))
        {
            logger.Log(level, "{SecurityEventType}: {Message}", eventType, message);
        }
    }

    /// <summary>
    /// Computes a safe hash of the request path for logging
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Safe hash for logging</returns>
    private static string ComputePathHash(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return "empty_path";

        // For common paths, return a safe identifier
        var safePaths = new Dictionary<string, string>
        {
            ["/"] = "root",
            ["/api/auth/login"] = "auth_login",
            ["/api/auth/logout"] = "auth_logout",
            ["/api/auth/register"] = "auth_register",
            ["/api/auth/profile"] = "auth_profile",
            ["/api/auth/refresh"] = "auth_refresh",
            ["/api/auth/session-info"] = "auth_session"
        };

        if (safePaths.TryGetValue(path, out var safePath))
        {
            return safePath;
        }

        // For other paths, return a hash
        var hash = path.GetHashCode();
        return $"path_hash_{Math.Abs(hash):X8}";
    }
}

/// <summary>
/// Types of security events - safe enum for logging
/// </summary>
public enum SecurityEventType
{
    Authentication = 1,
    Authorization = 2,
    TokenGeneration = 3,
    TokenValidation = 4,
    TokenRefresh = 5,
    TokenRevocation = 6,
    LoginAttempt = 7,
    LoginSuccess = 8,
    LoginFailure = 9,
    LogoutSuccess = 10,
    SessionCreated = 11,
    SessionExpired = 12,
    SecurityViolation = 13,
    SuspiciousActivity = 14,
    RateLimitExceeded = 15
}
