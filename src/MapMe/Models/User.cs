using System.Text.Json.Serialization;

namespace MapMe.Models;

/// <summary>
/// Represents a user account with authentication credentials
/// </summary>
/// <param name="Id">Unique identifier for the user account</param>
/// <param name="Username">Unique username for login</param>
/// <param name="Email">User's email address</param>
/// <param name="PasswordHash">Hashed password for authentication</param>
/// <param name="Salt">Salt used for password hashing</param>
/// <param name="GoogleId">Google OAuth ID if user signed up with Google</param>
/// <param name="IsEmailVerified">Whether the user's email has been verified</param>
/// <param name="IsActive">Whether the user account is active</param>
/// <param name="CreatedAt">When the user account was created</param>
/// <param name="UpdatedAt">When the user account was last updated</param>
/// <param name="LastLoginAt">When the user last logged in</param>
public record User(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("passwordHash")]
    string? PasswordHash,
    [property: JsonPropertyName("salt")] string? Salt,
    [property: JsonPropertyName("googleId")]
    string? GoogleId,
    [property: JsonPropertyName("isEmailVerified")]
    bool IsEmailVerified,
    [property: JsonPropertyName("isActive")]
    bool IsActive,
    [property: JsonPropertyName("createdAt")]
    DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updatedAt")]
    DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("lastLoginAt")]
    DateTimeOffset? LastLoginAt
);

/// <summary>
/// Represents an authenticated user session
/// </summary>
/// <param name="UserId">The user's unique identifier</param>
/// <param name="Username">The user's username</param>
/// <param name="Email">The user's email</param>
/// <param name="SessionId">Unique session identifier</param>
/// <param name="ExpiresAt">When the session expires</param>
/// <param name="CreatedAt">When the session was created</param>
public record UserSession(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("sessionId")]
    string SessionId,
    [property: JsonPropertyName("expiresAt")]
    DateTimeOffset ExpiresAt,
    [property: JsonPropertyName("createdAt")]
    DateTimeOffset CreatedAt
);