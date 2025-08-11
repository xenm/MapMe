using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapMe.Models;

namespace MapMe.Repositories;

public interface IDateMarkByUserRepository
{
    Task UpsertAsync(DateMark mark, CancellationToken ct = default);
    Task<DateMark?> GetByIdAsync(string userId, string id, CancellationToken ct = default);
    IAsyncEnumerable<DateMark> GetByUserAsync(string userId, DateOnly? from = null, DateOnly? to = null, IReadOnlyCollection<string>? categories = null, IReadOnlyCollection<string>? tags = null, IReadOnlyCollection<string>? qualities = null, CancellationToken ct = default);
}
