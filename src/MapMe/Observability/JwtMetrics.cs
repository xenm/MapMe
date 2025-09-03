using System.Diagnostics.Metrics;

namespace MapMe.Observability;

/// <summary>
/// Custom metrics collection for JWT authentication operations
/// </summary>
public class JwtMetrics
{
    private readonly Histogram<double> _authenticationDuration;
    private readonly Counter<int> _tokenGenerationCounter;
    private readonly Histogram<double> _tokenGenerationDuration;
    private readonly Counter<int> _tokenRefreshCounter;
    private readonly Counter<int> _tokenValidationCounter;
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

        _tokenRefreshCounter = meter.CreateCounter<int>(
            "jwt_tokens_refreshed_total",
            description: "Total number of JWT tokens refreshed");

        _tokenGenerationDuration = meter.CreateHistogram<double>(
            "jwt_token_generation_duration_ms",
            description: "Duration of JWT token generation in milliseconds");

        _tokenValidationDuration = meter.CreateHistogram<double>(
            "jwt_token_validation_duration_ms",
            description: "Duration of JWT token validation in milliseconds");

        _authenticationDuration = meter.CreateHistogram<double>(
            "jwt_authentication_duration_ms",
            description: "Duration of JWT authentication handler processing in milliseconds");
    }

    public void RecordTokenGeneration(double durationMs, bool success, string userId, bool rememberMe)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("success", success),
            new("remember_me", rememberMe)
        };

        _tokenGenerationCounter.Add(1, tags);
        _tokenGenerationDuration.Record(durationMs, tags);
    }

    public void RecordTokenValidation(double durationMs, bool success, string? result)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("success", success),
            new("result", result ?? "unknown")
        };

        _tokenValidationCounter.Add(1, tags);
        _tokenValidationDuration.Record(durationMs, tags);
    }

    public void RecordTokenRefresh(double durationMs, bool success, string? result)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("success", success),
            new("result", result ?? "unknown")
        };

        _tokenRefreshCounter.Add(1, tags);
    }

    public void RecordAuthentication(double durationMs, bool success, string? result, string requestPath)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("success", success),
            new("result", result ?? "unknown"),
            new("path", requestPath)
        };

        _authenticationDuration.Record(durationMs, tags);
    }
}