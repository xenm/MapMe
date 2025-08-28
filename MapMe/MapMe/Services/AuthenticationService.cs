using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace MapMe.Services;

/// <summary>
/// Service for user authentication and JWT token management
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IUserProfileRepository userProfileRepository,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
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

            // Generate JWT token
            var (token, expiresAt) = _jwtService.GenerateToken(user, request.RememberMe);

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
                token,
                expiresAt
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

            // Validate password strength
            var passwordValidationResult = ValidatePasswordStrength(request.Password);
            if (!passwordValidationResult.IsValid)
            {
                return new AuthenticationResponse(false, passwordValidationResult.ErrorMessage);
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

            // Generate JWT token
            var (token, expiresAt) = _jwtService.GenerateToken(user, false);

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
                token,
                expiresAt
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
                var (token, expiresAt) = _jwtService.GenerateToken(existingUser, false);

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
                    token,
                    expiresAt
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

                var (token, expiresAt) = _jwtService.GenerateToken(user, false);

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
                    token,
                    expiresAt
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return new AuthenticationResponse(false, "An error occurred during Google login");
        }
    }

    public async Task<bool> LogoutAsync(string token)
    {
        try
        {
            // JWT tokens are stateless, so logout is mainly for client-side cleanup
            // In a production system, you might want to maintain a blacklist of revoked tokens
            _logger.LogInformation("User logged out (JWT token invalidated client-side)");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
        }
    }

    public async Task<UserSession?> ValidateTokenAsync(string token)
    {
        try
        {
            return await Task.FromResult(_jwtService.ValidateToken(token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return null;
        }
    }

    public async Task<AuthenticatedUser?> GetCurrentUserAsync(string token)
    {
        try
        {
            var userSession = await ValidateTokenAsync(token);
            if (userSession == null) return null;

            var user = await _userRepository.GetByIdAsync(userSession.UserId);
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
            _logger.LogError(ex, "Error getting current user from JWT token");
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
            _logger.LogInformation("Password reset requested");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password reset");
            return false;
        }
    }

    public async Task<AuthenticationResponse> RefreshTokenAsync(string token)
    {
        try
        {
            var userSession = await ValidateTokenAsync(token);
            if (userSession == null)
            {
                return new AuthenticationResponse(false, "Invalid token");
            }

            var user = await _userRepository.GetByIdAsync(userSession.UserId);
            if (user == null || !user.IsActive)
            {
                return new AuthenticationResponse(false, "User not found or inactive");
            }

            var refreshResult = _jwtService.RefreshToken(token, user);
            if (refreshResult == null)
            {
                return new AuthenticationResponse(false, "Token does not need refresh yet");
            }

            var authenticatedUser = new AuthenticatedUser(
                user.Id,
                user.Username,
                user.Email,
                await GetDisplayNameAsync(user.Id),
                user.IsEmailVerified
            );

            return new AuthenticationResponse(
                true,
                "Token refreshed",
                authenticatedUser,
                refreshResult.Value.token,
                refreshResult.Value.expiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing JWT token");
            return new AuthenticationResponse(false, "An error occurred while refreshing token");
        }
    }

    #region Private Methods

    private static (string hash, string salt) HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[32];
        rng.GetBytes(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100000, HashAlgorithmName.SHA256, 32);
        var hash = Convert.ToBase64String(hashBytes);

        return (hash, salt);
    }

    private static bool VerifyPassword(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100000, HashAlgorithmName.SHA256, 32);
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

    /// <summary>
    /// Validates password strength according to security requirements
    /// </summary>
    private static PasswordValidationResult ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new PasswordValidationResult(false, "Password is required");
        }

        if (password.Length < 8)
        {
            return new PasswordValidationResult(false, "Password must be at least 8 characters long");
        }

        // Password is valid
        return new PasswordValidationResult(true, string.Empty);
    }

    #endregion
}

/// <summary>
/// Result of password validation
/// </summary>
internal record PasswordValidationResult(bool IsValid, string ErrorMessage);
