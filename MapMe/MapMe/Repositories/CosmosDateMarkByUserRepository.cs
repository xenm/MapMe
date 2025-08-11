using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MapMe.Models;
using Microsoft.Azure.Cosmos;

namespace MapMe.Repositories;

public sealed class CosmosDateMarkByUserRepository : IDateMarkByUserRepository
{
    private readonly CosmosClient _client;
    private readonly CosmosContextOptions _options;
    private Container Container => _client.GetContainer(_options.DatabaseName, "DateMarksByUser");

    public CosmosDateMarkByUserRepository(CosmosClient client, CosmosContextOptions options)
    {
        _client = client;
        _options = options;
    }

    public async Task UpsertAsync(DateMark mark, CancellationToken ct = default)
    {
        await Container.UpsertItemAsync(mark, new PartitionKey(mark.UserId), cancellationToken: ct);
    }

    public async Task<DateMark?> GetByIdAsync(string userId, string id, CancellationToken ct = default)
    {
        try
        {
            var resp = await Container.ReadItemAsync<DateMark>(id, new PartitionKey(userId), cancellationToken: ct);
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
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
        var sql = "SELECT * FROM c WHERE c.userId = @userId";
        var qd = new QueryDefinition(sql).WithParameter("@userId", userId);
        // Note: We'll keep filtering simple for now; advanced ARRAY_CONTAINS filters will be added later
        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId),
            MaxBufferedItemCount = 100,
            MaxConcurrency = 1,
            MaxItemCount = 100
        };
        using var it = Container.GetItemQueryIterator<DateMark>(qd, requestOptions: requestOptions);
        while (it.HasMoreResults)
        {
            var resp = await it.ReadNextAsync(ct);
            foreach (var item in resp)
            {
                if (item.IsDeleted) continue;
                if (from is not null && (item.VisitDate is null || item.VisitDate.Value < from.Value)) continue;
                if (to is not null && (item.VisitDate is null || item.VisitDate.Value > to.Value)) continue;
                if (categories is { Count: > 0 })
                {
                    var ok = false;
                    foreach (var c in item.CategoriesNorm)
                    {
                        if (categories.Contains(c)) { ok = true; break; }
                    }
                    if (!ok) continue;
                }
                if (tags is { Count: > 0 })
                {
                    var ok = false;
                    foreach (var t in item.TagsNorm)
                    {
                        if (tags.Contains(t)) { ok = true; break; }
                    }
                    if (!ok) continue;
                }
                if (qualities is { Count: > 0 })
                {
                    var ok = false;
                    foreach (var q in item.QualitiesNorm)
                    {
                        if (qualities.Contains(q)) { ok = true; break; }
                    }
                    if (!ok) continue;
                }
                yield return item;
            }
        }
    }
}
