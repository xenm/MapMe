using MapMe.Models;

namespace MapMe.Services;

/// <summary>
/// Service interface for JWT token operations
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a JWT token for the authenticated user
    /// </summary>
    /// <param name="user">The authenticated user</param>
    /// <param name="rememberMe">Whether to create a long-lived token</param>
    /// <returns>JWT token and expiration time</returns>
    (string token, DateTimeOffset expiresAt) GenerateToken(User user, bool rememberMe = false);
    
    /// <summary>
    /// Validates a JWT token and extracts user information
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>User session information if valid, null otherwise</returns>
    UserSession? ValidateToken(string token);
    
    /// <summary>
    /// Extracts user ID from a JWT token without full validation
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>User ID if extractable, null otherwise</returns>
    string? ExtractUserIdFromToken(string token);
    
    /// <summary>
    /// Refreshes a JWT token if it's still valid but near expiration
    /// </summary>
    /// <param name="token">The current JWT token</param>
    /// <param name="user">The user to refresh token for</param>
    /// <returns>New token and expiration if successful, null otherwise</returns>
    (string token, DateTimeOffset expiresAt)? RefreshToken(string token, User user);
}
