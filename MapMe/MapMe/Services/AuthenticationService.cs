using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace MapMe.Services;

/// <summary>
/// Service for user authentication and session management
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _userProfileRepository = userProfileRepository;
        _logger = logger;
    }

    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent username: {Username}", request.Username);
                return new AuthenticationResponse(false, "Invalid username or password");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt with deactivated account: {Username}", request.Username);
                return new AuthenticationResponse(false, "Account is deactivated");
            }

            if (user.PasswordHash == null || user.Salt == null)
            {
                _logger.LogWarning("Login attempt for OAuth-only account: {Username}", request.Username);
                return new AuthenticationResponse(false, "Please sign in with Google");
            }

            if (!VerifyPassword(request.Password, user.PasswordHash, user.Salt))
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
                return new AuthenticationResponse(false, "Invalid username or password");
            }

            // Update last login time
            await _userRepository.UpdateLastLoginAsync(user.Id, DateTimeOffset.UtcNow);

            // Create session
            var sessionDuration = request.RememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(24);
            var session = await _sessionRepository.CreateSessionAsync(user.Id, sessionDuration);

            var authenticatedUser = new AuthenticatedUser(
                user.Id,
                user.Username,
                user.Email,
                await GetDisplayNameAsync(user.Id),
                user.IsEmailVerified
            );

            _logger.LogInformation("Successful login for user: {Username}", request.Username);
            return new AuthenticationResponse(
                true,
                "Login successful",
                authenticatedUser,
                session.SessionId,
                session.ExpiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user: {Username}", request.Username);
            return new AuthenticationResponse(false, "An error occurred during login");
        }
    }

    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            if (!request.IsValid)
            {
                return new AuthenticationResponse(false, "Passwords do not match");
            }

            // Check username availability
            if (!await _userRepository.IsUsernameAvailableAsync(request.Username))
            {
                return new AuthenticationResponse(false, "Username is already taken");
            }

            // Check email availability
            if (!await _userRepository.IsEmailAvailableAsync(request.Email))
            {
                return new AuthenticationResponse(false, "Email is already registered");
            }

            // Hash password
            var (passwordHash, salt) = HashPassword(request.Password);
            var now = DateTimeOffset.UtcNow;
            var userId = Guid.NewGuid().ToString();

            // Create user
            var user = request.ToUser(userId, passwordHash, salt, now);
            await _userRepository.CreateAsync(user);

            // Create user profile
            await CreateDefaultUserProfileAsync(userId, request.Username, request.DisplayName);

            // Create session
            var session = await _sessionRepository.CreateSessionAsync(userId, TimeSpan.FromHours(24));

            var authenticatedUser = new AuthenticatedUser(
                user.Id,
                user.Username,
                user.Email,
                request.DisplayName,
                user.IsEmailVerified
            );

            _logger.LogInformation("New user registered: {Username}", request.Username);
            return new AuthenticationResponse(
                true,
                "Registration successful",
                authenticatedUser,
                session.SessionId,
                session.ExpiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user: {Username}", request.Username);
            return new AuthenticationResponse(false, "An error occurred during registration");
        }
    }

    public async Task<AuthenticationResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        try
        {
            // In a real implementation, you would verify the Google token here
            // For now, we'll trust the client-side verification

            var existingUser = await _userRepository.GetByGoogleIdAsync(request.GoogleId);
            if (existingUser != null)
            {
                // Existing Google user login
                if (!existingUser.IsActive)
                {
                    return new AuthenticationResponse(false, "Account is deactivated");
                }

                await _userRepository.UpdateLastLoginAsync(existingUser.Id, DateTimeOffset.UtcNow);
                var session = await _sessionRepository.CreateSessionAsync(existingUser.Id, TimeSpan.FromHours(24));

                var authenticatedUser = new AuthenticatedUser(
                    existingUser.Id,
                    existingUser.Username,
                    existingUser.Email,
                    await GetDisplayNameAsync(existingUser.Id),
                    existingUser.IsEmailVerified
                );

                return new AuthenticationResponse(
                    true,
                    "Login successful",
                    authenticatedUser,
                    session.SessionId,
                    session.ExpiresAt
                );
            }
            else
            {
                // New Google user registration
                var emailUser = await _userRepository.GetByEmailAsync(request.Email);
                if (emailUser != null)
                {
                    return new AuthenticationResponse(false, "An account with this email already exists");
                }

                // Generate unique username from display name
                var username = await GenerateUniqueUsernameAsync(request.DisplayName);
                var now = DateTimeOffset.UtcNow;
                var userId = Guid.NewGuid().ToString();

                var user = new User(
                    userId,
                    username,
                    request.Email,
                    null, // No password for Google users
                    null, // No salt for Google users
                    request.GoogleId,
                    true, // Email is verified through Google
                    true,
                    now,
                    now,
                    now
                );

                await _userRepository.CreateAsync(user);
                await CreateDefaultUserProfileAsync(userId, username, request.DisplayName);

                var session = await _sessionRepository.CreateSessionAsync(userId, TimeSpan.FromHours(24));

                var authenticatedUser = new AuthenticatedUser(
                    user.Id,
                    user.Username,
                    user.Email,
                    request.DisplayName,
                    user.IsEmailVerified
                );

                _logger.LogInformation("New Google user registered: {Username}", username);
                return new AuthenticationResponse(
                    true,
                    "Registration successful",
                    authenticatedUser,
                    session.SessionId,
                    session.ExpiresAt
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login for email: {Email}", request.Email);
            return new AuthenticationResponse(false, "An error occurred during Google login");
        }
    }

    public async Task<bool> LogoutAsync(string sessionId)
    {
        try
        {
            await _sessionRepository.InvalidateSessionAsync(sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for session: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<UserSession?> ValidateSessionAsync(string sessionId)
    {
        try
        {
            return await _sessionRepository.GetValidSessionAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<AuthenticatedUser?> GetCurrentUserAsync(string sessionId)
    {
        try
        {
            var session = await ValidateSessionAsync(sessionId);
            if (session == null) return null;

            var user = await _userRepository.GetByIdAsync(session.UserId);
            if (user == null || !user.IsActive) return null;

            return new AuthenticatedUser(
                user.Id,
                user.Username,
                user.Email,
                await GetDisplayNameAsync(user.Id),
                user.IsEmailVerified
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user for session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        try
        {
            if (!request.IsValid)
            {
                return false;
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.PasswordHash == null || user.Salt == null)
            {
                return false;
            }

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash, user.Salt))
            {
                return false;
            }

            var (newPasswordHash, newSalt) = HashPassword(request.NewPassword);
            var updatedUser = user with
            {
                PasswordHash = newPasswordHash,
                Salt = newSalt,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _userRepository.UpdateAsync(updatedUser);
            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> RequestPasswordResetAsync(PasswordResetRequest request)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal if email exists or not
                return true;
            }

            // In a real implementation, you would send a password reset email here
            _logger.LogInformation("Password reset requested for email: {Email}", request.Email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password reset for email: {Email}", request.Email);
            return false;
        }
    }

    public async Task<AuthenticationResponse> RefreshSessionAsync(string sessionId)
    {
        try
        {
            var session = await ValidateSessionAsync(sessionId);
            if (session == null)
            {
                return new AuthenticationResponse(false, "Invalid session");
            }

            var newSession = await _sessionRepository.RefreshSessionAsync(sessionId, TimeSpan.FromHours(24));
            if (newSession == null)
            {
                return new AuthenticationResponse(false, "Unable to refresh session");
            }

            var user = await GetCurrentUserAsync(newSession.SessionId);
            return new AuthenticationResponse(
                true,
                "Session refreshed",
                user,
                newSession.SessionId,
                newSession.ExpiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing session: {SessionId}", sessionId);
            return new AuthenticationResponse(false, "An error occurred while refreshing session");
        }
    }

    #region Private Methods

    private static (string hash, string salt) HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[32];
        rng.GetBytes(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(32);
        var hash = Convert.ToBase64String(hashBytes);

        return (hash, salt);
    }

    private static bool VerifyPassword(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(32);
            var computedHash = Convert.ToBase64String(hashBytes);
            return computedHash == hash;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GenerateUniqueUsernameAsync(string displayName)
    {
        // Clean display name to create base username
        var baseUsername = displayName
            .ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");

        // Remove non-alphanumeric characters
        var cleanUsername = new string(baseUsername.Where(char.IsLetterOrDigit).ToArray());
        
        if (string.IsNullOrEmpty(cleanUsername))
        {
            cleanUsername = "user";
        }

        // Ensure minimum length
        if (cleanUsername.Length < 3)
        {
            cleanUsername = cleanUsername.PadRight(3, '0');
        }

        // Check if base username is available
        if (await _userRepository.IsUsernameAvailableAsync(cleanUsername))
        {
            return cleanUsername;
        }

        // Add numbers until we find an available username
        for (int i = 1; i <= 9999; i++)
        {
            var candidateUsername = $"{cleanUsername}{i}";
            if (await _userRepository.IsUsernameAvailableAsync(candidateUsername))
            {
                return candidateUsername;
            }
        }

        // Fallback to GUID if all else fails
        return Guid.NewGuid().ToString("N")[..8];
    }

    private async Task CreateDefaultUserProfileAsync(string userId, string username, string displayName)
    {
        var defaultProfile = new UserProfile(
            Id: Guid.NewGuid().ToString(),
            UserId: userId,
            DisplayName: displayName,
            Bio: "New MapMe user",
            Photos: new List<UserPhoto>().AsReadOnly(),
            Preferences: new UserPreferences(
                Categories: new List<string>().AsReadOnly()
            ),
            Visibility: "public",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        await _userProfileRepository.UpsertAsync(defaultProfile);
    }

    private async Task<string> GetDisplayNameAsync(string userId)
    {
        try
        {
            var profile = await _userProfileRepository.GetByUserIdAsync(userId);
            return profile?.DisplayName ?? "User";
        }
        catch
        {
            return "User";
        }
    }

    #endregion
}
