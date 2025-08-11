using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MapMe.Models;

namespace MapMe.Repositories;

public sealed class InMemoryDateMarkByUserRepository : IDateMarkByUserRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateMark>> _byUser = new();
    private readonly ConcurrentDictionary<string, DateMark> _byId = new();

    public Task UpsertAsync(DateMark mark, CancellationToken ct = default)
    {
        var userDict = _byUser.GetOrAdd(mark.UserId, _ => new());
        userDict[mark.Id] = mark;
        _byId[mark.Id] = mark;
        return Task.CompletedTask;
    }

    public Task<DateMark?> GetByIdAsync(string userId, string id, CancellationToken ct = default)
    {
        if (_byUser.TryGetValue(userId, out var dict) && dict.TryGetValue(id, out var dm))
            return Task.FromResult<DateMark?>(dm);
        return Task.FromResult<DateMark?>(null);
    }

    public async IAsyncEnumerable<DateMark> GetByUserAsync(
        string userId,
        DateOnly? from = null,
        DateOnly? to = null,
        IReadOnlyCollection<string>? categories = null,
        IReadOnlyCollection<string>? tags = null,
        IReadOnlyCollection<string>? qualities = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_byUser.TryGetValue(userId, out var dict)) yield break;
        IEnumerable<DateMark> q = dict.Values.Where(m => !m.IsDeleted);
        if (from is not null) q = q.Where(m => m.VisitDate is not null && m.VisitDate.Value >= from.Value);
        if (to is not null) q = q.Where(m => m.VisitDate is not null && m.VisitDate.Value <= to.Value);
        if (categories is { Count: > 0 }) q = q.Where(m => m.CategoriesNorm.Any(categories.Contains));
        if (tags is { Count: > 0 }) q = q.Where(m => m.TagsNorm.Any(tags.Contains));
        if (qualities is { Count: > 0 }) q = q.Where(m => m.QualitiesNorm.Any(qualities.Contains));
        foreach (var item in q.OrderByDescending(m => m.CreatedAt))
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}
