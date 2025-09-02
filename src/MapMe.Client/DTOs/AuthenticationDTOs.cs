using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MapMe.Client.DTOs;

/// <summary>
/// Request DTO for user login
/// </summary>
public class LoginRequest
{
    [JsonPropertyName("username")]
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = "";

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = "";

    [JsonPropertyName("rememberMe")] public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Request DTO for user registration
/// </summary>
public class RegisterRequest
{
    [JsonPropertyName("username")]
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$",
        ErrorMessage = "Username can only contain letters, numbers, hyphens, and underscores")]
    public string Username { get; set; } = "";

    [JsonPropertyName("email")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    public string Password { get; set; } = "";

    [JsonPropertyName("confirmPassword")]
    [Required(ErrorMessage = "Please confirm your password")]
    public string ConfirmPassword { get; set; } = "";

    [JsonPropertyName("displayName")]
    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 100 characters")]
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Validates that password and confirm password match
    /// </summary>
    public bool IsValid => Password == ConfirmPassword;
}

/// <summary>
/// Request DTO for Google OAuth login
/// </summary>
public class GoogleLoginRequest
{
    [JsonPropertyName("googleToken")]
    [Required(ErrorMessage = "Google token is required")]
    public string GoogleToken { get; set; } = "";

    [JsonPropertyName("email")]
    [Required(ErrorMessage = "Email is required")]
    public string Email { get; set; } = "";

    [JsonPropertyName("displayName")]
    [Required(ErrorMessage = "Display name is required")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("googleId")]
    [Required(ErrorMessage = "Google ID is required")]
    public string GoogleId { get; set; } = "";
}

/// <summary>
/// Response DTO for successful authentication
/// </summary>
public class AuthenticationResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; } = "";

    [JsonPropertyName("user")] public AuthenticatedUser? User { get; set; }

    [JsonPropertyName("token")] public string? Token { get; set; }

    [JsonPropertyName("expiresAt")] public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>
/// Authenticated user information for client
/// </summary>
public class AuthenticatedUser
{
    [JsonPropertyName("userId")] public string UserId { get; set; } = "";

    [JsonPropertyName("username")] public string Username { get; set; } = "";

    [JsonPropertyName("email")] public string Email { get; set; } = "";

    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = "";

    [JsonPropertyName("isEmailVerified")] public bool IsEmailVerified { get; set; }
}

/// <summary>
/// Request DTO for password reset
/// </summary>
public class PasswordResetRequest
{
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = "";
}

/// <summary>
/// Request DTO for changing password
/// </summary>
public class ChangePasswordRequest
{
    [JsonPropertyName("currentPassword")]
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = "";

    [JsonPropertyName("newPassword")]
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    public string NewPassword { get; set; } = "";

    [JsonPropertyName("confirmNewPassword")]
    [Required(ErrorMessage = "Please confirm your new password")]
    public string ConfirmNewPassword { get; set; } = "";

    /// <summary>
    /// Validates that new password and confirm password match
    /// </summary>
    public bool IsValid => NewPassword == ConfirmNewPassword;
}

/// <summary>
/// Request DTO for logout
/// </summary>
public class LogoutRequest
{
    [JsonPropertyName("token")] public string? Token { get; set; }
}