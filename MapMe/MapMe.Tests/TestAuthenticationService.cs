using MapMe.Services;
using MapMe.Models;
using MapMe.DTOs;

namespace MapMe.Tests;

/// <summary>
/// Test implementation of authentication service that bypasses real authentication
/// for integration testing purposes
/// </summary>
public class TestAuthenticationService : IAuthenticationService
{
    public Task<AuthenticationResponse> LoginAsync(LoginRequest request)
    {
        // Return successful authentication for any login request
        var response = new AuthenticationResponse(
            Success: true,
            Message: "Login successful",
            User: new AuthenticatedUser(
                UserId: "u_test",
                Username: request.Username,
                Email: $"{request.Username}@test.com",
                DisplayName: request.Username,
                IsEmailVerified: true
            ),
            SessionId: "test-session-token",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24)
        );
        
        return Task.FromResult(response);
    }

    public Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
    {
        // Return successful registration for any request
        var response = new AuthenticationResponse(
            Success: true,
            Message: "Registration successful",
            User: new AuthenticatedUser(
                UserId: $"u_{request.Username}",
                Username: request.Username,
                Email: request.Email,
                DisplayName: request.DisplayName,
                IsEmailVerified: false
            ),
            SessionId: "test-session-token",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24)
        );
        
        return Task.FromResult(response);
    }

    public Task<AuthenticationResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        // Return successful Google authentication
        var response = new AuthenticationResponse(
            Success: true,
            Message: "Google login successful",
            User: new AuthenticatedUser(
                UserId: "u_test_google",
                Username: request.Email.Split('@')[0],
                Email: request.Email,
                DisplayName: request.DisplayName,
                IsEmailVerified: true
            ),
            SessionId: "test-session-token",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24)
        );
        
        return Task.FromResult(response);
    }

    public Task<bool> LogoutAsync(string sessionId)
    {
        // Always return successful logout
        return Task.FromResult(true);
    }

    public Task<UserSession?> ValidateSessionAsync(string sessionId)
    {
        // Return valid session for any session token that starts with "test-"
        if (!string.IsNullOrEmpty(sessionId) && sessionId.StartsWith("test-"))
        {
            // Use a generic test user ID that works for all tests
            var session = new UserSession(
                UserId: "test_user_id",
                Username: "test_user",
                Email: "test@example.com",
                SessionId: sessionId,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(23),
                CreatedAt: DateTimeOffset.UtcNow.AddHours(-1)
            );
            return Task.FromResult<UserSession?>(session);
        }
        
        return Task.FromResult<UserSession?>(null);
    }

    public Task<AuthenticatedUser?> GetCurrentUserAsync(string sessionId)
    {
        // Return test user for any session token that starts with "test-"
        if (!string.IsNullOrEmpty(sessionId) && sessionId.StartsWith("test-"))
        {
            var user = new AuthenticatedUser(
                UserId: "test_user_id",
                Username: "test_user",
                Email: "test@example.com",
                DisplayName: "Test User",
                IsEmailVerified: true
            );
            return Task.FromResult<AuthenticatedUser?>(user);
        }
        
        return Task.FromResult<AuthenticatedUser?>(null);
    }

    public Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        // Always return successful password change
        return Task.FromResult(true);
    }

    public Task<bool> RequestPasswordResetAsync(PasswordResetRequest request)
    {
        // Always return successful password reset request
        return Task.FromResult(true);
    }

    public Task<AuthenticationResponse> RefreshSessionAsync(string sessionId)
    {
        // Return successful session refresh
        var response = new AuthenticationResponse(
            Success: true,
            Message: "Session refreshed",
            User: new AuthenticatedUser(
                UserId: "u_test",
                Username: "test_user",
                Email: "test@example.com",
                DisplayName: "Test User",
                IsEmailVerified: true
            ),
            SessionId: "test-session-token-refreshed",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24)
        );
        
        return Task.FromResult(response);
    }
}
