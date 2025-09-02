using System.Collections.Concurrent;
using MapMe.Models;

namespace MapMe.Repositories;

/// <summary>
/// In-memory implementation of IUserRepository for development and testing
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, string> _userIdsByEmail = new();
    private readonly ConcurrentDictionary<string, string> _userIdsByGoogleId = new();
    private readonly ConcurrentDictionary<string, string> _userIdsByUsername = new();
    private readonly ConcurrentDictionary<string, User> _usersById = new();

    public Task<User?> GetByIdAsync(string id)
    {
        _usersById.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        if (_userIdsByUsername.TryGetValue(username.ToLowerInvariant(), out var userId))
        {
            return GetByIdAsync(userId);
        }

        return Task.FromResult<User?>(null);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        if (_userIdsByEmail.TryGetValue(email.ToLowerInvariant(), out var userId))
        {
            return GetByIdAsync(userId);
        }

        return Task.FromResult<User?>(null);
    }

    public Task<User?> GetByGoogleIdAsync(string googleId)
    {
        if (_userIdsByGoogleId.TryGetValue(googleId, out var userId))
        {
            return GetByIdAsync(userId);
        }

        return Task.FromResult<User?>(null);
    }

    public Task<User> CreateAsync(User user)
    {
        if (_usersById.ContainsKey(user.Id))
        {
            throw new InvalidOperationException($"User with ID {user.Id} already exists");
        }

        if (_userIdsByUsername.ContainsKey(user.Username.ToLowerInvariant()))
        {
            throw new InvalidOperationException($"Username {user.Username} is already taken");
        }

        if (_userIdsByEmail.ContainsKey(user.Email.ToLowerInvariant()))
        {
            throw new InvalidOperationException($"Email {user.Email} is already registered");
        }

        _usersById[user.Id] = user;
        _userIdsByUsername[user.Username.ToLowerInvariant()] = user.Id;
        _userIdsByEmail[user.Email.ToLowerInvariant()] = user.Id;

        if (!string.IsNullOrEmpty(user.GoogleId))
        {
            _userIdsByGoogleId[user.GoogleId] = user.Id;
        }

        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        if (!_usersById.ContainsKey(user.Id))
        {
            throw new InvalidOperationException($"User with ID {user.Id} does not exist");
        }

        var existingUser = _usersById[user.Id];

        // Update username mapping if changed
        if (existingUser.Username != user.Username)
        {
            _userIdsByUsername.TryRemove(existingUser.Username.ToLowerInvariant(), out _);
            _userIdsByUsername[user.Username.ToLowerInvariant()] = user.Id;
        }

        // Update email mapping if changed
        if (existingUser.Email != user.Email)
        {
            _userIdsByEmail.TryRemove(existingUser.Email.ToLowerInvariant(), out _);
            _userIdsByEmail[user.Email.ToLowerInvariant()] = user.Id;
        }

        // Update Google ID mapping if changed
        if (existingUser.GoogleId != user.GoogleId)
        {
            if (!string.IsNullOrEmpty(existingUser.GoogleId))
            {
                _userIdsByGoogleId.TryRemove(existingUser.GoogleId, out _);
            }

            if (!string.IsNullOrEmpty(user.GoogleId))
            {
                _userIdsByGoogleId[user.GoogleId] = user.Id;
            }
        }

        _usersById[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<bool> IsUsernameAvailableAsync(string username)
    {
        var isAvailable = !_userIdsByUsername.ContainsKey(username.ToLowerInvariant());
        return Task.FromResult(isAvailable);
    }

    public Task<bool> IsEmailAvailableAsync(string email)
    {
        var isAvailable = !_userIdsByEmail.ContainsKey(email.ToLowerInvariant());
        return Task.FromResult(isAvailable);
    }

    public async Task UpdateLastLoginAsync(string userId, DateTimeOffset lastLoginAt)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            var updatedUser = user with { LastLoginAt = lastLoginAt, UpdatedAt = DateTimeOffset.UtcNow };
            await UpdateAsync(updatedUser);
        }
    }

    public async Task DeactivateAsync(string userId)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            var updatedUser = user with { IsActive = false, UpdatedAt = DateTimeOffset.UtcNow };
            await UpdateAsync(updatedUser);
        }
    }

    public async Task ActivateAsync(string userId)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            var updatedUser = user with { IsActive = true, UpdatedAt = DateTimeOffset.UtcNow };
            await UpdateAsync(updatedUser);
        }
    }
}