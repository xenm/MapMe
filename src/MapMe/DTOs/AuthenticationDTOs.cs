using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MapMe.Models;

namespace MapMe.DTOs;

/// <summary>
/// Request DTO for user login
/// </summary>
public record LoginRequest(
    [property: JsonPropertyName("username")]
    [Required(ErrorMessage = "Username is required")]
    string Username,
    [property: JsonPropertyName("password")]
    [Required(ErrorMessage = "Password is required")]
    string Password,
    [property: JsonPropertyName("rememberMe")]
    bool RememberMe = false
);

/// <summary>
/// Request DTO for user registration
/// </summary>
public record RegisterRequest(
    [property: JsonPropertyName("username")]
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$",
        ErrorMessage = "Username can only contain letters, numbers, hyphens, and underscores")]
    string Username,
    [property: JsonPropertyName("email")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    string Email,
    [property: JsonPropertyName("password")]
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    string Password,
    [property: JsonPropertyName("confirmPassword")]
    [Required(ErrorMessage = "Please confirm your password")]
    string ConfirmPassword,
    [property: JsonPropertyName("displayName")]
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 100 characters")]
    string DisplayName
)
{
    /// <summary>
    /// Validates that password and confirm password match
    /// </summary>
    public bool IsValid => Password == ConfirmPassword;

    /// <summary>
    /// Converts to User model for creation
    /// </summary>
    public User ToUser(string id, string passwordHash, string salt, DateTimeOffset now) => new(
        Id: id,
        Username: Username,
        Email: Email,
        PasswordHash: passwordHash,
        Salt: salt,
        GoogleId: null,
        IsEmailVerified: false,
        IsActive: true,
        CreatedAt: now,
        UpdatedAt: now,
        LastLoginAt: null
    );
}

/// <summary>
/// Request DTO for Google OAuth login
/// </summary>
public record GoogleLoginRequest(
    [property: JsonPropertyName("googleToken")]
    [Required(ErrorMessage = "Google token is required")]
    string GoogleToken,
    [property: JsonPropertyName("email")]
    [Required(ErrorMessage = "Email is required")]
    string Email,
    [property: JsonPropertyName("displayName")]
    [Required(ErrorMessage = "Display name is required")]
    string DisplayName,
    [property: JsonPropertyName("googleId")]
    [Required(ErrorMessage = "Google ID is required")]
    string GoogleId,
    [property: JsonPropertyName("picture")]
    string? Picture
);

/// <summary>
/// Response DTO for successful authentication
/// </summary>
public record AuthenticationResponse(
    [property: JsonPropertyName("success")]
    bool Success,
    [property: JsonPropertyName("message")]
    string Message,
    [property: JsonPropertyName("user")] AuthenticatedUser? User = null,
    [property: JsonPropertyName("token")] string? Token = null,
    [property: JsonPropertyName("expiresAt")]
    DateTimeOffset? ExpiresAt = null,
    [property: JsonPropertyName("isNewUser")]
    bool IsNewUser = false
);

/// <summary>
/// Authenticated user information for client
/// </summary>
public record AuthenticatedUser(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("username")]
    string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("displayName")]
    string DisplayName,
    [property: JsonPropertyName("isEmailVerified")]
    bool IsEmailVerified
);

/// <summary>
/// Request DTO for password reset
/// </summary>
public record PasswordResetRequest(
    [property: JsonPropertyName("email")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    string Email
);

/// <summary>
/// Request DTO for changing password
/// </summary>
public record ChangePasswordRequest(
    [property: JsonPropertyName("currentPassword")]
    [Required(ErrorMessage = "Current password is required")]
    string CurrentPassword,
    [property: JsonPropertyName("newPassword")]
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    string NewPassword,
    [property: JsonPropertyName("confirmNewPassword")]
    [Required(ErrorMessage = "Please confirm your new password")]
    string ConfirmNewPassword
)
{
    /// <summary>
    /// Validates that new password and confirm password match
    /// </summary>
    public bool IsValid => NewPassword == ConfirmNewPassword;
}

/// <summary>
/// Request DTO for logout
/// </summary>
public record LogoutRequest(
    [property: JsonPropertyName("token")] string? Token = null
);