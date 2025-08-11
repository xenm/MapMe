using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapMe.Models;

namespace MapMe.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task UpsertAsync(UserProfile profile, CancellationToken ct = default);
}
