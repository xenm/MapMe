using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Diagnostics;
using MapMe.Services;
using MapMe.Models;

namespace MapMe.Authentication;

/// <summary>
/// Custom authentication handler for JWT tokens
/// </summary>
public class JwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<JwtAuthenticationHandler> _logger;

    public JwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IJwtService jwtService)
        : base(options, logger, encoder)
    {
        _jwtService = jwtService;
        _logger = logger.CreateLogger<JwtAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        using var activity = Activity.Current?.Source.StartActivity("JwtAuthenticationHandler.HandleAuthenticate");
        var startTime = DateTimeOffset.UtcNow;
        var requestPath = Request.Path.Value ?? "[unknown]";
        var requestMethod = Request.Method;
        var userAgent = Request.Headers["User-Agent"].ToString();
        var clientIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "[unknown]";
        
        activity?.SetTag("http.method", requestMethod);
        activity?.SetTag("http.path", requestPath);
        activity?.SetTag("client.ip", clientIp);
        activity?.SetTag("user_agent", userAgent);
        
        try
        {
            // Check for Authorization header with Bearer token
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                activity?.SetTag("auth.result", "no_header");
                _logger.LogDebug(
                    "No Authorization header found. Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    requestPath, requestMethod, clientIp);
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Handle multiple Authorization headers by using the first one
            var authHeaders = Request.Headers["Authorization"];
            if (authHeaders.Count > 1)
            {
                _logger.LogWarning(
                    "Multiple Authorization headers detected. Using first header. Count: {Count}, Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    authHeaders.Count, requestPath, requestMethod, clientIp);
            }
            
            var authHeader = authHeaders.FirstOrDefault() ?? "";
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                activity?.SetTag("auth.result", "invalid_scheme");
                _logger.LogDebug(
                    "Invalid authorization scheme. Expected Bearer, got: {Scheme}. Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    authHeader.Split(' ').FirstOrDefault() ?? "[empty]", requestPath, requestMethod, clientIp);
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            if (string.IsNullOrEmpty(token))
            {
                activity?.SetTag("auth.result", "empty_token");
                _logger.LogWarning(
                    "Empty Bearer token provided. Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    requestPath, requestMethod, clientIp);
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            var tokenPreview = token.Length > 20 ? $"{token[..20]}..." : "[short-token]";
            activity?.SetTag("token.preview", tokenPreview);

            // Validate the JWT token
            UserSession? userSession;
            try
            {
                userSession = _jwtService.ValidateToken(token);
            }
            catch (Exception jwtEx)
            {
                activity?.SetTag("auth.result", "jwt_validation_error");
                activity?.SetTag("jwt.error.type", jwtEx.GetType().Name);
                _logger.LogInformation(jwtEx,
                    "JWT token validation threw exception. TokenPreview: {TokenPreview}, Path: {Path}, Method: {Method}, ClientIP: {ClientIP}, UserAgent: {UserAgent}",
                    tokenPreview, requestPath, requestMethod, clientIp, userAgent);
                return Task.FromResult(AuthenticateResult.Fail("Token validation failed"));
            }
            
            if (userSession == null)
            {
                activity?.SetTag("auth.result", "invalid_token");
                _logger.LogInformation(
                    "JWT token validation failed. TokenPreview: {TokenPreview}, Path: {Path}, Method: {Method}, ClientIP: {ClientIP}, UserAgent: {UserAgent}",
                    tokenPreview, requestPath, requestMethod, clientIp, userAgent);
                return Task.FromResult(AuthenticateResult.Fail("Invalid or expired token"));
            }

            // Create claims identity
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userSession.UserId),
                new Claim(ClaimTypes.Name, userSession.Username),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim("userId", userSession.UserId),
                new Claim("username", userSession.Username),
                new Claim("email", userSession.Email),
                new Claim("sessionId", userSession.SessionId),
                new Claim("expiresAt", userSession.ExpiresAt.ToString("O"))
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("auth.result", "success");
            activity?.SetTag("user.id", userSession.UserId);
            activity?.SetTag("user.username", userSession.Username);
            activity?.SetTag("token.id", userSession.SessionId);
            activity?.SetTag("token.expires_at", userSession.ExpiresAt.ToString("O"));
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));

            _logger.LogDebug(
                "JWT authentication successful. UserId: {UserId}, Username: {Username}, TokenId: {TokenId}, Path: {Path}, Method: {Method}, ClientIP: {ClientIP}, Duration: {Duration}ms",
                userSession.UserId, userSession.Username, userSession.SessionId, requestPath, requestMethod, clientIp, duration.TotalMilliseconds);
                
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("auth.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            _logger.LogError(ex,
                "Unexpected error during JWT authentication. Path: {Path}, Method: {Method}, ClientIP: {ClientIP}, Duration: {Duration}ms, Error: {ErrorType}",
                requestPath, requestMethod, clientIp, duration.TotalMilliseconds, ex.GetType().Name);
                
            return Task.FromResult(AuthenticateResult.Fail("Authentication error"));
        }
    }
}
