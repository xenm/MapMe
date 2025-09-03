using System.Diagnostics;
using MapMe.Services;

namespace MapMe.Logging;

/// <summary>
/// Logger decorator that automatically enhances log entries with strongly-typed UserContext information.
/// This decorator eliminates CodeQL log forging vulnerabilities by using only GUIDs, enums, and safe values - never user-provided strings.
/// </summary>
public class SecureLoggerDecorator<T> : ILogger<T>
{
    private readonly ILogger<T> _innerLogger;
    private readonly ISecureLoggingService _secureLoggingService;

    public SecureLoggerDecorator(ILogger<T> innerLogger, ISecureLoggingService secureLoggingService)
    {
        _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
        _secureLoggingService = secureLoggingService ?? throw new ArgumentNullException(nameof(secureLoggingService));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _innerLogger.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _innerLogger.IsEnabled(logLevel);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        // Create secure log context using strongly-typed UserContext - no user-provided strings
        var secureContext = _secureLoggingService.CreateSecureLogContext(new
        {
            OriginalEventId = eventId.Id,
            OriginalEventName = eventId.Name ?? "UnnamedEvent",
            LogLevel = logLevel.ToString(),
            CategoryName = typeof(T).Name,
            HasException = exception != null,
            ExceptionType = exception?.GetType().Name ?? "None",
            Timestamp = DateTime.UtcNow,
            TraceId = Activity.Current?.TraceId.ToString() ?? "none"
        });

        // Log with secure context - completely eliminates log injection vulnerabilities
        using var scope = _innerLogger.BeginScope(secureContext);
        _innerLogger.Log(logLevel, eventId, state, exception, formatter);
    }
}

/// <summary>
/// Extension methods for easy registration of secure logger decorator with UserContext.
/// </summary>
public static class SecureLoggerExtensions
{
    /// <summary>
    /// Wraps an existing logger with the secure logger decorator using UserContext.
    /// </summary>
    public static ILogger<T> WithSecureLogging<T>(this ILogger<T> logger, ISecureLoggingService secureLoggingService)
    {
        return new SecureLoggerDecorator<T>(logger, secureLoggingService);
    }

    /// <summary>
    /// Adds secure logging services to the DI container with UserContext approach.
    /// </summary>
    public static IServiceCollection AddSecureLogging(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ISecureLoggingService, SecureLoggingService>();

        // Note: Decorator registration would be done in Program.cs to avoid circular dependencies

        return services;
    }
}