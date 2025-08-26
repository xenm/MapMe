using MapMe.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Diagnostics;

namespace MapMe.Services;

/// <summary>
/// Service for JWT token operations
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Get JWT configuration from appsettings
        _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        _issuer = _configuration["Jwt:Issuer"] ?? "MapMe";
        _audience = _configuration["Jwt:Audience"] ?? "MapMe";
        
        // Validate secret key length
        if (_secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
        }
    }

    public (string token, DateTimeOffset expiresAt) GenerateToken(User user, bool rememberMe = false)
    {
        using var activity = Activity.Current?.Source.StartActivity("JwtService.GenerateToken");
        activity?.SetTag("user.id", user.Id);
        activity?.SetTag("user.username", user.Username);
        activity?.SetTag("remember_me", rememberMe.ToString());
        
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);
            
            // Set expiration based on remember me option
            var expirationTime = rememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(24);
            var expiresAt = DateTimeOffset.UtcNow.Add(expirationTime);
            var tokenId = Guid.NewGuid().ToString();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("userId", user.Id),
                    new Claim("username", user.Username),
                    new Claim("isEmailVerified", user.IsEmailVerified.ToString()),
                    new Claim("isActive", user.IsActive.ToString()),
                    new Claim("googleId", user.GoogleId ?? ""),
                    new Claim("tokenId", tokenId) // Unique token identifier
                }),
                Expires = expiresAt.DateTime,
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("token.id", tokenId);
            activity?.SetTag("token.expires_at", expiresAt.ToString("O"));
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));

            _logger.LogInformation(
                "JWT token generated successfully. UserId: {UserId}, Username: {Username}, TokenId: {TokenId}, ExpiresAt: {ExpiresAt}, RememberMe: {RememberMe}, Duration: {Duration}ms",
                user.Id, user.Username, tokenId, expiresAt, rememberMe, duration.TotalMilliseconds);
                
            return (tokenString, expiresAt);
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            _logger.LogError(ex, 
                "Failed to generate JWT token. UserId: {UserId}, Username: {Username}, RememberMe: {RememberMe}, Duration: {Duration}ms, Error: {ErrorType}",
                user.Id, user.Username, rememberMe, duration.TotalMilliseconds, ex.GetType().Name);
            throw;
        }
    }

    public UserSession? ValidateToken(string token)
    {
        using var activity = Activity.Current?.Source.StartActivity("JwtService.ValidateToken");
        var startTime = DateTimeOffset.UtcNow;
        var tokenPreview = token.Length > 20 ? $"{token[..20]}..." : "[short-token]";
        
        activity?.SetTag("token.preview", tokenPreview);
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // Extract claims
            var userId = principal.FindFirst("userId")?.Value;
            var username = principal.FindFirst("username")?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var tokenId = principal.FindFirst("tokenId")?.Value;
            var jwtToken = (JwtSecurityToken)validatedToken;
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(jwtToken.Payload.Expiration ?? 0);
            var createdAt = jwtToken.Payload.IssuedAt;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
            {
                activity?.SetTag("validation.result", "missing_claims");
                _logger.LogWarning(
                    "JWT token validation failed due to missing required claims. TokenId: {TokenId}, UserId: {UserId}, Username: {Username}, Email: {Email}",
                    tokenId, userId ?? "[missing]", username ?? "[missing]", email ?? "[missing]");
                return null;
            }

            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("user.id", userId);
            activity?.SetTag("user.username", username);
            activity?.SetTag("token.id", tokenId);
            activity?.SetTag("token.expires_at", expiresAt.ToString("O"));
            activity?.SetTag("validation.result", "success");
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));

            _logger.LogDebug(
                "JWT token validated successfully. UserId: {UserId}, Username: {Username}, TokenId: {TokenId}, ExpiresAt: {ExpiresAt}, Duration: {Duration}ms",
                userId, username, tokenId, expiresAt, duration.TotalMilliseconds);

            // Create UserSession equivalent for JWT
            return new UserSession(
                UserId: userId,
                Username: username,
                Email: email,
                SessionId: tokenId ?? Guid.NewGuid().ToString(), // Use tokenId as sessionId equivalent
                ExpiresAt: expiresAt,
                CreatedAt: createdAt
            );
        }
        catch (SecurityTokenExpiredException)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("validation.result", "expired");
            activity?.SetTag("error.type", "TokenExpired");
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            _logger.LogInformation(
                "JWT token validation failed - token expired. TokenPreview: {TokenPreview}, Duration: {Duration}ms",
                tokenPreview, duration.TotalMilliseconds);
            return null;
        }
        catch (SecurityTokenException ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("validation.result", "invalid");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            _logger.LogWarning(ex, 
                "JWT token validation failed - invalid token. TokenPreview: {TokenPreview}, Duration: {Duration}ms, Error: {ErrorType}, Message: {ErrorMessage}",
                tokenPreview, duration.TotalMilliseconds, ex.GetType().Name, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("validation.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            _logger.LogError(ex, 
                "Unexpected error during JWT token validation. TokenPreview: {TokenPreview}, Duration: {Duration}ms, Error: {ErrorType}",
                tokenPreview, duration.TotalMilliseconds, ex.GetType().Name);
            return null;
        }
    }

    public string? ExtractUserIdFromToken(string token)
    {
        using var activity = Activity.Current?.Source.StartActivity("JwtService.ExtractUserIdFromToken");
        var tokenPreview = token.Length > 20 ? $"{token[..20]}..." : "[short-token]";
        activity?.SetTag("token.preview", tokenPreview);
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Read token without validation to extract claims
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var userId = jsonToken.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;
            
            activity?.SetTag("user.id", userId ?? "[not-found]");
            activity?.SetTag("extraction.result", userId != null ? "success" : "not_found");
            
            if (userId != null)
            {
                _logger.LogDebug("Successfully extracted UserId from JWT token. UserId: {UserId}", userId);
            }
            else
            {
                _logger.LogDebug("UserId claim not found in JWT token. TokenPreview: {TokenPreview}", tokenPreview);
            }
            
            return userId;
        }
        catch (Exception ex)
        {
            activity?.SetTag("extraction.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            
            _logger.LogDebug(ex, 
                "Error extracting user ID from JWT token. TokenPreview: {TokenPreview}, Error: {ErrorType}",
                tokenPreview, ex.GetType().Name);
            return null;
        }
    }

    public (string token, DateTimeOffset expiresAt)? RefreshToken(string token, User user)
    {
        using var activity = Activity.Current?.Source.StartActivity("JwtService.RefreshToken");
        var startTime = DateTimeOffset.UtcNow;
        var tokenPreview = token.Length > 20 ? $"{token[..20]}..." : "[short-token]";
        
        activity?.SetTag("user.id", user.Id);
        activity?.SetTag("user.username", user.Username);
        activity?.SetTag("token.preview", tokenPreview);
        
        try
        {
            var userSession = ValidateToken(token);
            if (userSession == null)
            {
                activity?.SetTag("refresh.result", "invalid_token");
                _logger.LogInformation(
                    "Token refresh failed - invalid or expired token. UserId: {UserId}, TokenPreview: {TokenPreview}",
                    user.Id, tokenPreview);
                return null;
            }

            // Check if token is within refresh window (e.g., expires within next hour)
            var refreshWindow = TimeSpan.FromHours(1);
            var timeUntilExpiry = userSession.ExpiresAt - DateTimeOffset.UtcNow;
            
            if (userSession.ExpiresAt > DateTimeOffset.UtcNow.Add(refreshWindow))
            {
                activity?.SetTag("refresh.result", "not_needed");
                activity?.SetTag("time_until_expiry_minutes", timeUntilExpiry.TotalMinutes.ToString("F2"));
                
                _logger.LogDebug(
                    "Token refresh not needed - token still valid for {TimeUntilExpiry} minutes. UserId: {UserId}, TokenId: {TokenId}",
                    timeUntilExpiry.TotalMinutes, user.Id, userSession.SessionId);
                return null;
            }

            // Generate new token with same remember me logic based on original expiration
            var originalDuration = userSession.ExpiresAt - userSession.CreatedAt;
            var rememberMe = originalDuration > TimeSpan.FromHours(25); // Assume remember me if > 25 hours
            
            var refreshResult = GenerateToken(user, rememberMe);
            var duration = DateTimeOffset.UtcNow - startTime;
            
            activity?.SetTag("refresh.result", "success");
            activity?.SetTag("remember_me", rememberMe.ToString());
            activity?.SetTag("original_token_id", userSession.SessionId);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            _logger.LogInformation(
                "JWT token refreshed successfully. UserId: {UserId}, Username: {Username}, OriginalTokenId: {OriginalTokenId}, RememberMe: {RememberMe}, Duration: {Duration}ms",
                user.Id, user.Username, userSession.SessionId, rememberMe, duration.TotalMilliseconds);

            return refreshResult;
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("refresh.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            _logger.LogError(ex, 
                "Error refreshing JWT token. UserId: {UserId}, Username: {Username}, TokenPreview: {TokenPreview}, Duration: {Duration}ms, Error: {ErrorType}",
                user.Id, user.Username, tokenPreview, duration.TotalMilliseconds, ex.GetType().Name);
            return null;
        }
    }
}
