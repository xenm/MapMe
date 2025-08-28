using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MapMe.Services;

namespace MapMe.Authentication;

/// <summary>
/// Custom authentication handler that integrates our session-based authentication
/// with ASP.NET Core's authentication middleware
/// </summary>
public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly MapMe.Services.IAuthenticationService _authService;
    private readonly ISecureLoggingService _secureLoggingService;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        MapMe.Services.IAuthenticationService authService,
        ISecureLoggingService secureLoggingService)
        : base(options, logger, encoder)
    {
        _authService = authService;
        _secureLoggingService = secureLoggingService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Get session ID from request headers (used by API endpoints)
            var sessionId = GetSessionIdFromRequest();
            if (string.IsNullOrEmpty(sessionId))
            {
                return AuthenticateResult.NoResult();
            }

            // Validate session using our authentication service
            var user = await _authService.GetCurrentUserAsync(sessionId);
            if (user == null)
            {
                return AuthenticateResult.Fail("Invalid session");
            }

            // Create claims principal for ASP.NET Core authentication
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("DisplayName", user.DisplayName ?? user.Username)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            // Log authentication error using secure UserContext approach - only safe values
            var loggerFactory = Context.RequestServices.GetRequiredService<ILoggerFactory>();
            var typedLogger = loggerFactory.CreateLogger<SessionAuthenticationHandler>();
            _secureLoggingService.LogSecurityEvent(typedLogger, LogLevel.Error, SecurityEventType.Authentication,
                "Error during authentication", new
                {
                    AuthResult = "Error",
                    ErrorType = ex.GetType().Name,
                    HttpMethod = Request.Method
                });
            return AuthenticateResult.Fail("Authentication error");
        }
    }

    /// <summary>
    /// Gets the session ID from the request headers
    /// </summary>
    private string? GetSessionIdFromRequest()
    {
        // Try X-Session-Id header first (used by client-side requests)
        var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(sessionId))
        {
            return sessionId;
        }

        // Try Authorization header with Bearer token format
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length);
        }

        // Try cookie-based session (for server-side rendered pages)
        var cookie = Request.Cookies["MapMe-Session"];
        if (!string.IsNullOrEmpty(cookie))
        {
            return cookie;
        }

        return null;
    }
}
