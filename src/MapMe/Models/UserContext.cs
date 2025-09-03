using System.Security.Claims;

namespace MapMe.Models;

/// <summary>
/// Strongly-typed user context for secure logging that eliminates log injection vulnerabilities.
/// Contains only GUIDs, enums, and other non-string values that cannot be manipulated for log forging.
/// </summary>
public sealed class UserContext
{
    /// <summary>
    /// Unique user identifier - safe for logging as it's a GUID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// JWT token identifier - safe for logging as it's a GUID
    /// </summary>
    public Guid TokenId { get; init; }

    /// <summary>
    /// Session identifier - safe for logging as it's a GUID
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// Authentication method used - safe for logging as it's an enum
    /// </summary>
    public AuthenticationMethod AuthMethod { get; init; }

    /// <summary>
    /// User role - safe for logging as it's an enum
    /// </summary>
    public UserRole Role { get; init; }

    /// <summary>
    /// Whether email is verified - safe for logging as it's a boolean
    /// </summary>
    public bool IsEmailVerified { get; init; }

    /// <summary>
    /// Account creation timestamp - safe for logging as it's a DateTime
    /// </summary>
    public DateTime AccountCreatedAt { get; init; }

    /// <summary>
    /// Last login timestamp - safe for logging as it's a DateTime
    /// </summary>
    public DateTime LastLoginAt { get; init; }

    /// <summary>
    /// Token expiration timestamp - safe for logging as it's a DateTime
    /// </summary>
    public DateTime TokenExpiresAt { get; init; }

    /// <summary>
    /// Whether this is a "remember me" session - safe for logging as it's a boolean
    /// </summary>
    public bool IsRememberMe { get; init; }

    /// <summary>
    /// Client IP address hash - safe for logging as it's a computed hash
    /// </summary>
    public string ClientIpHash { get; init; } = string.Empty;

    /// <summary>
    /// User agent hash - safe for logging as it's a computed hash
    /// </summary>
    public string UserAgentHash { get; init; } = string.Empty;

    /// <summary>
    /// Request correlation ID - safe for logging as it's a GUID
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Creates a UserContext from JWT claims, extracting only safe, strongly-typed values
    /// </summary>
    /// <param name="claims">JWT claims principal</param>
    /// <param name="httpContext">HTTP context for additional safe metadata</param>
    /// <returns>Strongly-typed UserContext safe for logging</returns>
    public static UserContext FromClaims(ClaimsPrincipal? claims, HttpContext? httpContext = null)
    {
        if (claims?.Identity?.IsAuthenticated != true)
        {
            return CreateAnonymous(httpContext);
        }

        // Extract only safe, strongly-typed values from claims
        var userIdClaim = claims.FindFirst("sub") ?? claims.FindFirst(ClaimTypes.NameIdentifier);
        var tokenIdClaim = claims.FindFirst("jti");
        var sessionIdClaim = claims.FindFirst("sid");
        var roleClaim = claims.FindFirst(ClaimTypes.Role);
        var emailVerifiedClaim = claims.FindFirst("email_verified");
        var authMethodClaim = claims.FindFirst("auth_method");

        // Parse GUIDs safely - if parsing fails, use empty GUID
        Guid.TryParse(userIdClaim?.Value, out var userId);
        Guid.TryParse(tokenIdClaim?.Value, out var tokenId);
        Guid.TryParse(sessionIdClaim?.Value, out var sessionId);

        // Parse enums safely - if parsing fails, use default values
        Enum.TryParse<UserRole>(roleClaim?.Value, true, out var role);
        Enum.TryParse<AuthenticationMethod>(authMethodClaim?.Value, true, out var authMethod);

        // Parse boolean safely
        bool.TryParse(emailVerifiedClaim?.Value, out var isEmailVerified);

        // Parse timestamps safely
        var iatClaim = claims.FindFirst("iat");
        var expClaim = claims.FindFirst("exp");

        var issuedAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddHours(1);

        if (long.TryParse(iatClaim?.Value, out var iat))
        {
            issuedAt = DateTimeOffset.FromUnixTimeSeconds(iat).DateTime;
        }

        if (long.TryParse(expClaim?.Value, out var exp))
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
        }

        return new UserContext
        {
            UserId = userId,
            TokenId = tokenId,
            SessionId = sessionId,
            AuthMethod = authMethod,
            Role = role,
            IsEmailVerified = isEmailVerified,
            AccountCreatedAt = issuedAt,
            LastLoginAt = DateTime.UtcNow,
            TokenExpiresAt = expiresAt,
            IsRememberMe = claims.HasClaim("remember_me", "true"),
            ClientIpHash = ComputeHash(httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
            UserAgentHash = ComputeHash(httpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown"),
            CorrelationId = GetOrCreateCorrelationId(httpContext)
        };
    }

    /// <summary>
    /// Creates an anonymous UserContext for unauthenticated requests
    /// </summary>
    /// <param name="httpContext">HTTP context for safe metadata</param>
    /// <returns>Anonymous UserContext safe for logging</returns>
    public static UserContext CreateAnonymous(HttpContext? httpContext = null)
    {
        return new UserContext
        {
            UserId = Guid.Empty,
            TokenId = Guid.Empty,
            SessionId = Guid.Empty,
            AuthMethod = AuthenticationMethod.None,
            Role = UserRole.Anonymous,
            IsEmailVerified = false,
            AccountCreatedAt = DateTime.MinValue,
            LastLoginAt = DateTime.MinValue,
            TokenExpiresAt = DateTime.MinValue,
            IsRememberMe = false,
            ClientIpHash = ComputeHash(httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
            UserAgentHash = ComputeHash(httpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown"),
            CorrelationId = GetOrCreateCorrelationId(httpContext)
        };
    }

    /// <summary>
    /// Creates a structured logging object with all safe values for logging
    /// </summary>
    /// <returns>Anonymous object safe for structured logging</returns>
    public object ToLogContext()
    {
        return new
        {
            UserId,
            TokenId,
            SessionId,
            AuthMethod = AuthMethod.ToString(),
            Role = Role.ToString(),
            IsEmailVerified,
            AccountCreatedAt,
            LastLoginAt,
            TokenExpiresAt,
            IsRememberMe,
            ClientIpHash,
            UserAgentHash,
            CorrelationId
        };
    }

    /// <summary>
    /// Computes a safe hash of potentially unsafe string values
    /// </summary>
    /// <param name="value">Value to hash</param>
    /// <returns>Safe hash string for logging</returns>
    private static string ComputeHash(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "empty";

        // Use a simple hash that's safe for logging
        var hash = value.GetHashCode();
        return $"hash_{Math.Abs(hash):X8}";
    }

    /// <summary>
    /// Gets or creates a correlation ID for request tracking
    /// </summary>
    /// <param name="httpContext">HTTP context</param>
    /// <returns>Correlation ID GUID</returns>
    private static Guid GetOrCreateCorrelationId(HttpContext? httpContext)
    {
        if (httpContext?.Items.TryGetValue("CorrelationId", out var correlationObj) == true
            && correlationObj is Guid existingId)
        {
            return existingId;
        }

        var newId = Guid.NewGuid();
        if (httpContext != null)
        {
            httpContext.Items["CorrelationId"] = newId;
        }

        return newId;
    }
}

/// <summary>
/// Authentication methods - safe enum for logging
/// </summary>
public enum AuthenticationMethod
{
    None = 0,
    JWT = 1,
    Session = 2,
    Google = 3,
    ApiKey = 4
}

/// <summary>
/// User roles - safe enum for logging
/// </summary>
public enum UserRole
{
    Anonymous = 0,
    User = 1,
    Moderator = 2,
    Admin = 3,
    System = 4
}