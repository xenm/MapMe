using MapMe.DTOs;
using MapMe.Models;

namespace MapMe.Services;

/// <summary>
/// Service interface for user authentication and session management
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    Task<AuthenticationResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Registers a new user account
    /// </summary>
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates a user with Google OAuth
    /// </summary>
    Task<AuthenticationResponse> GoogleLoginAsync(GoogleLoginRequest request);

    /// <summary>
    /// Logs out a user (JWT tokens are stateless, so this is mainly for client cleanup)
    /// </summary>
    Task<bool> LogoutAsync(string token);

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    Task<UserSession?> ValidateTokenAsync(string token);

    /// <summary>
    /// Gets the current authenticated user from JWT token
    /// </summary>
    Task<AuthenticatedUser?> GetCurrentUserAsync(string token);

    /// <summary>
    /// Changes a user's password
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    Task<bool> RequestPasswordResetAsync(PasswordResetRequest request);

    /// <summary>
    /// Refreshes a JWT token
    /// </summary>
    Task<AuthenticationResponse> RefreshTokenAsync(string token);
}