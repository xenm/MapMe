using MapMe.Models;
using MapMe.Services;
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
    private readonly ISecureLoggingService _secureLoggingService;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger, ISecureLoggingService secureLoggingService)
    {
        _configuration = configuration;
        _logger = logger;
        _secureLoggingService = secureLoggingService;
        
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
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User cannot be null");
        }

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

            // Log using secure UserContext approach - only GUIDs and safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Information, SecurityEventType.TokenGeneration,
                "JWT token generated successfully", new
                {
                    TokenId = tokenId,
                    ExpiresAt = expiresAt,
                    RememberMe = rememberMe,
                    DurationMs = duration.TotalMilliseconds,
                    TokenType = "JWT"
                });
                
            return (tokenString, expiresAt);
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            // Log error using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Error, SecurityEventType.TokenGeneration,
                "Failed to generate JWT token", new
                {
                    RememberMe = rememberMe,
                    DurationMs = duration.TotalMilliseconds,
                    ErrorType = ex.GetType().Name,
                    TokenType = "JWT"
                });
            throw;
        }
    }

    public UserSession? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        using var activity = Activity.Current?.Source.StartActivity("JwtService.ValidateToken");
        var startTime = DateTimeOffset.UtcNow;
        
        // No longer log token previews - use only safe correlation IDs
        activity?.SetTag("operation.type", "token_validation");
        
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
                // Log validation failure using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Warning, SecurityEventType.TokenValidation,
                    "JWT token validation failed due to missing required claims", new
                    {
                        TokenId = tokenId,
                        HasEmail = !string.IsNullOrEmpty(email),
                        EmailLength = email?.Length ?? 0,
                        ValidationFailureReason = "MissingRequiredClaims"
                    });
                return null;
            }

            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("user.id", userId);
            activity?.SetTag("user.username", username);
            activity?.SetTag("token.id", tokenId);
            activity?.SetTag("token.expires_at", expiresAt.ToString("O"));
            activity?.SetTag("validation.result", "success");
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));

            // Log validation success using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.TokenValidation,
                "JWT token validated successfully", new
                {
                    TokenId = tokenId,
                    ExpiresAt = expiresAt,
                    DurationMs = duration.TotalMilliseconds,
                    ValidationResult = "Success"
                });

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
            
            // Log token expiration using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Information, SecurityEventType.TokenValidation,
                "JWT token validation failed - token expired", new
                {
                    ValidationResult = "Expired",
                    DurationMs = duration.TotalMilliseconds,
                    FailureReason = "TokenExpired"
                });
            return null;
        }
        catch (SecurityTokenException ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("validation.result", "invalid");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            // Log token validation failure using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Warning, SecurityEventType.TokenValidation,
                "JWT token validation failed - invalid token", new
                {
                    ValidationResult = "Invalid",
                    DurationMs = duration.TotalMilliseconds,
                    ErrorType = ex.GetType().Name,
                    FailureReason = "InvalidToken"
                });
            return null;
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("validation.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            // Log unexpected validation error using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Error, SecurityEventType.TokenValidation,
                "Unexpected error during JWT token validation", new
                {
                    ValidationResult = "Error",
                    DurationMs = duration.TotalMilliseconds,
                    ErrorType = ex.GetType().Name,
                    FailureReason = "UnexpectedError"
                });
            return null;
        }
    }

    public string? ExtractUserIdFromToken(string token)
    {
        using var activity = Activity.Current?.Source.StartActivity("JwtService.ExtractUserIdFromToken");
        activity?.SetTag("operation.type", "extract_user_id");
        
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
                // Log success using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.TokenValidation,
                    "Successfully extracted UserId from JWT token", new
                    {
                        ExtractionResult = "Success",
                        HasUserId = true
                    });
            }
            else
            {
                // Log failure using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.TokenValidation,
                    "UserId claim not found in JWT token", new
                    {
                        ExtractionResult = "NotFound",
                        FailureReason = "UserIdClaimNotFound"
                    });
            }
            
            return userId;
        }
        catch (Exception ex)
        {
            activity?.SetTag("extraction.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            
            // Log error using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.TokenValidation,
                "Error extracting user ID from JWT token", new
                {
                    ExtractionResult = "Error",
                    ErrorType = ex.GetType().Name
                });
            return null;
        }
    }

    public (string token, DateTimeOffset expiresAt)? RefreshToken(string token, User user)
    {
        using var activity = Activity.Current?.Source.StartActivity("JwtService.RefreshToken");
        var startTime = DateTimeOffset.UtcNow;
        
        activity?.SetTag("user.id", user.Id);
        activity?.SetTag("user.username", user.Username);
        activity?.SetTag("operation.type", "token_refresh");
        
        try
        {
            var userSession = ValidateToken(token);
            if (userSession == null)
            {
                activity?.SetTag("refresh.result", "invalid_token");
                // Log refresh failure using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Information, SecurityEventType.TokenRefresh,
                    "Token refresh failed - invalid or expired token", new
                    {
                        RefreshResult = "InvalidToken",
                        FailureReason = "TokenInvalidOrExpired"
                    });
                return null;
            }

            // Check if token is within refresh window (e.g., expires within next hour)
            var refreshWindow = TimeSpan.FromHours(1);
            var timeUntilExpiry = userSession.ExpiresAt - DateTimeOffset.UtcNow;
            
            if (userSession.ExpiresAt > DateTimeOffset.UtcNow.Add(refreshWindow))
            {
                activity?.SetTag("refresh.result", "not_needed");
                activity?.SetTag("time_until_expiry_minutes", timeUntilExpiry.TotalMinutes.ToString("F2"));
                
                // Log refresh not needed using secure UserContext approach - only safe values
                _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Debug, SecurityEventType.TokenRefresh,
                    "Token refresh not needed - token still valid", new
                    {
                        RefreshResult = "NotNeeded",
                        TimeUntilExpiryMinutes = timeUntilExpiry.TotalMinutes,
                        TokenId = userSession.SessionId
                    });
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
            
            // Log refresh success using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Information, SecurityEventType.TokenRefresh,
                "JWT token refreshed successfully", new
                {
                    RefreshResult = "Success",
                    OriginalTokenId = userSession.SessionId,
                    RememberMe = rememberMe,
                    DurationMs = duration.TotalMilliseconds
                });

            return refreshResult;
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            activity?.SetTag("refresh.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString("F2"));
            
            // Log refresh error using secure UserContext approach - only safe values
            _secureLoggingService.LogSecurityEvent(_logger, LogLevel.Error, SecurityEventType.TokenRefresh,
                "Error refreshing JWT token", new
                {
                    RefreshResult = "Error",
                    DurationMs = duration.TotalMilliseconds,
                    ErrorType = ex.GetType().Name
                });
            return null;
        }
    }
}
