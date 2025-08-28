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
    private readonly ISecureLoggingService _secureLoggingService;
    private readonly ILogger<JwtAuthenticationHandler> _logger;

    public JwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IJwtService jwtService,
        ISecureLoggingService secureLoggingService)
        : base(options, logger, encoder)
    {
        _jwtService = jwtService;
        _secureLoggingService = secureLoggingService;
        _logger = logger.CreateLogger<JwtAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        using var activity = Activity.Current?.Source.StartActivity("JwtAuthenticationHandler.HandleAuthenticate");
        var startTime = DateTimeOffset.UtcNow;
        
        activity?.SetTag("http.method", Request.Method);
        activity?.SetTag("http.path", Request.Path.Value ?? "/");
        activity?.SetTag("operation.type", "jwt_authentication");
        
        try
        {
            // Check for Authorization header with Bearer token
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                activity?.SetTag("auth.result", "no_header");
                // Log missing authorization header using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.Authentication,
                    "No Authorization header found", new
                    {
                        AuthResult = "NoHeader",
                        HttpMethod = Request.Method,
                        HasPath = !string.IsNullOrEmpty(Request.Path.Value)
                    });
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Handle multiple Authorization headers - reject for security
            var authHeaders = Request.Headers["Authorization"];
            if (authHeaders.Count > 1)
            {
                activity?.SetTag("auth.result", "multiple_headers");
                // Log multiple authorization headers using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Warning, SecurityEventType.Authentication,
                    "Multiple Authorization headers detected - rejecting for security", new
                    {
                        AuthResult = "MultipleHeaders",
                        HeaderCount = authHeaders.Count,
                        HttpMethod = Request.Method,
                        SecurityThreat = "MultipleAuthHeaders"
                    });
                return Task.FromResult(AuthenticateResult.Fail("Multiple Authorization headers not allowed"));
            }
            
            var authHeader = authHeaders.FirstOrDefault() ?? "";
            if (!authHeader.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                activity?.SetTag("auth.result", "invalid_scheme");
                // Log invalid authorization scheme using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.Authentication,
                    "Invalid authorization scheme - expected Bearer", new
                    {
                        AuthResult = "InvalidScheme",
                        ExpectedScheme = "Bearer",
                        HttpMethod = Request.Method,
                        HasAuthHeader = !string.IsNullOrEmpty(authHeader)
                    });
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Handle case where header is just "Bearer" without space or token
            if (authHeader.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                activity?.SetTag("auth.result", "bearer_without_token");
                // Log Bearer header without token using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Warning, SecurityEventType.Authentication,
                    "Bearer authorization header without token", new
                    {
                        AuthResult = "BearerWithoutToken",
                        HttpMethod = Request.Method,
                        SecurityIssue = "MissingToken"
                    });
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            // Extract token using case-insensitive parsing
            string token;
            var bearerIndex = authHeader.IndexOf(' ');
            
            // Log authorization header parsing using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.Authentication,
                "Parsing Authorization header", new
                {
                    AuthResult = "ParsingHeader",
                    HasSpaceInHeader = bearerIndex > 0,
                    HttpMethod = Request.Method
                });
            
            if (bearerIndex == -1 || bearerIndex != 6) // "Bearer" is 6 characters, space should be at index 6
            {
                activity?.SetTag("auth.result", "invalid_bearer_format");
                // Log invalid Bearer format using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Warning, SecurityEventType.Authentication,
                    "Invalid Bearer format - expected 'Bearer <token>'", new
                    {
                        AuthResult = "InvalidBearerFormat",
                        SpaceIndex = bearerIndex,
                        HttpMethod = Request.Method,
                        SecurityIssue = "MalformedAuthHeader"
                    });
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            token = authHeader.Substring(bearerIndex + 1).Trim();
            // Log token extraction using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.Authentication,
                "Extracted token from header", new
                {
                    AuthResult = "TokenExtracted",
                    TokenLength = token?.Length ?? 0,
                    HasToken = !string.IsNullOrEmpty(token),
                    HttpMethod = Request.Method
                });
            
            if (string.IsNullOrEmpty(token))
            {
                activity?.SetTag("auth.result", "empty_token");
                // Log empty Bearer token using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Warning, SecurityEventType.Authentication,
                    "Empty Bearer token provided", new
                    {
                        AuthResult = "EmptyToken",
                        HttpMethod = Request.Method,
                        SecurityIssue = "EmptyToken"
                    });
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            // No longer log token previews - use only safe correlation IDs
            activity?.SetTag("operation.type", "jwt_validation");

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
                // Log JWT validation exception using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Information, SecurityEventType.Authentication,
                    "JWT token validation threw exception", new
                    {
                        AuthResult = "ValidationException",
                        ErrorType = jwtEx.GetType().Name,
                        HttpMethod = Request.Method
                    });
                return Task.FromResult(AuthenticateResult.Fail("Token validation failed"));
            }
            
            if (userSession == null)
            {
                activity?.SetTag("auth.result", "invalid_token");
                // Log JWT validation failure using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Information, SecurityEventType.Authentication,
                    "JWT token validation failed", new
                    {
                        AuthResult = "InvalidToken",
                        HttpMethod = Request.Method,
                        ValidationResult = "Failed"
                    });
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

            // Log JWT authentication success using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.Authentication,
                "JWT authentication successful", new
                {
                    AuthResult = "Success",
                    TokenId = userSession.SessionId,
                    HttpMethod = Request.Method,
                    DurationMs = duration.TotalMilliseconds
                });
                
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("auth.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            // Log unexpected authentication error using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Error, SecurityEventType.Authentication,
                "Unexpected error during JWT authentication", new
                {
                    AuthResult = "UnexpectedError",
                    DurationMs = duration.TotalMilliseconds,
                    ErrorType = ex.GetType().Name,
                    HttpMethod = Request.Method
                });
                
            return Task.FromResult(AuthenticateResult.Fail("Authentication error"));
        }
    }

}
