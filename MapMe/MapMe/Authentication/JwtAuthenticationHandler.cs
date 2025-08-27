using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Diagnostics;
using MapMe.Services;
using MapMe.Models;
using MapMe.Utilities;

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
        var requestPath = SecureLogging.SanitizePathForLog(Request.Path.Value);
        var requestMethod = SecureLogging.SanitizeForLog(Request.Method, maxLength: 10);
        var userAgent = SecureLogging.SanitizeHeaderForLog(Request.Headers["User-Agent"].ToString(), "User-Agent");
        var clientIp = SecureLogging.SanitizeForLog(Request.HttpContext.Connection.RemoteIpAddress?.ToString(), maxLength: 45, placeholder: "[unknown-ip]");
        
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

            // Handle multiple Authorization headers - reject for security
            var authHeaders = Request.Headers["Authorization"];
            if (authHeaders.Count > 1)
            {
                activity?.SetTag("auth.result", "multiple_headers");
                _logger.LogWarning(
                    "Multiple Authorization headers detected - rejecting for security. Count: {Count}, Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    authHeaders.Count, requestPath, requestMethod, clientIp);
                return Task.FromResult(AuthenticateResult.Fail("Multiple Authorization headers not allowed"));
            }
            
            var authHeader = authHeaders.FirstOrDefault() ?? "";
            if (!authHeader.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                activity?.SetTag("auth.result", "invalid_scheme");
                _logger.LogDebug(
                    "Invalid authorization scheme. Expected Bearer, got: {Scheme}. Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    SecureLogging.SanitizeForLog(authHeader.Split(' ').FirstOrDefault() ?? "[empty]", maxLength: 20), requestPath, requestMethod, clientIp);
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Handle case where header is just "Bearer" without space or token
            if (authHeader.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                activity?.SetTag("auth.result", "bearer_without_token");
                _logger.LogWarning(
                    "Bearer authorization header without token. Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    requestPath, requestMethod, clientIp);
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            // Extract token using case-insensitive parsing
            string token;
            var bearerIndex = authHeader.IndexOf(' ');
            
            _logger.LogDebug(
                "Parsing Authorization header. Scheme: '{Scheme}', SpaceIndex: {SpaceIndex}, Path: {Path}",
                SecureLogging.SanitizeForLog(authHeader.Split(' ').FirstOrDefault() ?? "[empty]", maxLength: 20), bearerIndex, requestPath);
            
            if (bearerIndex == -1 || bearerIndex != 6) // "Bearer" is 6 characters, space should be at index 6
            {
                activity?.SetTag("auth.result", "invalid_bearer_format");
                _logger.LogWarning(
                    "Invalid Bearer format. Expected 'Bearer <token>'. SpaceIndex: {SpaceIndex}, Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    bearerIndex, requestPath, requestMethod, clientIp);
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            token = authHeader.Substring(bearerIndex + 1).Trim();
            _logger.LogDebug(
                "Extracted token from header. TokenLength: {TokenLength}, Path: {Path}",
                token?.Length ?? 0, requestPath);
            if (string.IsNullOrEmpty(token))
            {
                activity?.SetTag("auth.result", "empty_token");
                _logger.LogWarning(
                    "Empty Bearer token provided. Path: {Path}, Method: {Method}, ClientIP: {ClientIP}",
                    requestPath, requestMethod, clientIp);
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            var tokenPreview = SecureLogging.ToTokenPreview(token);
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
