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
    /// Logs out a user and invalidates their session
    /// </summary>
    Task<bool> LogoutAsync(string sessionId);
    
    /// <summary>
    /// Validates a user session
    /// </summary>
    Task<UserSession?> ValidateSessionAsync(string sessionId);
    
    /// <summary>
    /// Gets the current authenticated user from session
    /// </summary>
    Task<AuthenticatedUser?> GetCurrentUserAsync(string sessionId);
    
    /// <summary>
    /// Changes a user's password
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    
    /// <summary>
    /// Initiates password reset process
    /// </summary>
    Task<bool> RequestPasswordResetAsync(PasswordResetRequest request);
    
    /// <summary>
    /// Refreshes a user session
    /// </summary>
    Task<AuthenticationResponse> RefreshSessionAsync(string sessionId);
}
