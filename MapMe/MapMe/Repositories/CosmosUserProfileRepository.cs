using System;
using System.Threading;
using System.Threading.Tasks;
using MapMe.Models;
using Microsoft.Azure.Cosmos;

namespace MapMe.Repositories;

public sealed class CosmosUserProfileRepository : IUserProfileRepository
{
    private readonly CosmosClient _client;
    private readonly CosmosContextOptions _options;
    private Container _container => _client.GetContainer(_options.DatabaseName, "Users");

    public CosmosUserProfileRepository(CosmosClient client, CosmosContextOptions options)
    {
        _client = client;
        _options = options;
    }

    public async Task<UserProfile?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var resp = await _container.ReadItemAsync<UserProfile>(id, new PartitionKey(id), cancellationToken: ct);
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var q = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId").WithParameter("@userId", userId);
        using var feed = _container.GetItemQueryIterator<UserProfile>(q, requestOptions: new QueryRequestOptions
        {
            PartitionKey = null,
            MaxItemCount = 1
        });
        if (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync(ct);
            foreach (var item in page) return item;
        }
        return null;
    }

    public async Task UpsertAsync(UserProfile profile, CancellationToken ct = default)
    {
        await _container.UpsertItemAsync(profile, new PartitionKey(profile.Id), cancellationToken: ct);
    }
}
