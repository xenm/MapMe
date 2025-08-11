using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MapMe.Models;

namespace MapMe.Repositories;

public sealed class InMemoryUserProfileRepository : IUserProfileRepository
{
    private readonly ConcurrentDictionary<string, UserProfile> _byId = new();
    private readonly ConcurrentDictionary<string, string> _userToProfileId = new();

    public Task<UserProfile?> GetByIdAsync(string id, CancellationToken ct = default)
        => Task.FromResult(_byId.TryGetValue(id, out var p) ? p : null);

    public Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        if (_userToProfileId.TryGetValue(userId, out var id) && _byId.TryGetValue(id, out var p))
            return Task.FromResult<UserProfile?>(p);
        return Task.FromResult<UserProfile?>(null);
    }

    public Task UpsertAsync(UserProfile profile, CancellationToken ct = default)
    {
        _byId[profile.Id] = profile;
        _userToProfileId[profile.UserId] = profile.Id;
        return Task.CompletedTask;
    }
}
