using MapMe.Models;

namespace MapMe.Repositories;

/// <summary>
/// Repository interface for user account management
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique ID
    /// </summary>
    Task<User?> GetByIdAsync(string id);
    
    /// <summary>
    /// Gets a user by their username
    /// </summary>
    Task<User?> GetByUsernameAsync(string username);
    
    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    Task<User?> GetByEmailAsync(string email);
    
    /// <summary>
    /// Gets a user by their Google ID
    /// </summary>
    Task<User?> GetByGoogleIdAsync(string googleId);
    
    /// <summary>
    /// Creates a new user account
    /// </summary>
    Task<User> CreateAsync(User user);
    
    /// <summary>
    /// Updates an existing user account
    /// </summary>
    Task<User> UpdateAsync(User user);
    
    /// <summary>
    /// Checks if a username is already taken
    /// </summary>
    Task<bool> IsUsernameAvailableAsync(string username);
    
    /// <summary>
    /// Checks if an email is already registered
    /// </summary>
    Task<bool> IsEmailAvailableAsync(string email);
    
    /// <summary>
    /// Updates the user's last login time
    /// </summary>
    Task UpdateLastLoginAsync(string userId, DateTimeOffset lastLoginAt);
    
    /// <summary>
    /// Deactivates a user account
    /// </summary>
    Task DeactivateAsync(string userId);
    
    /// <summary>
    /// Activates a user account
    /// </summary>
    Task ActivateAsync(string userId);
}
