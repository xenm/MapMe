using System;
using System.Threading;
using System.Threading.Tasks;
using MapMe.Models;
using MapMe.Data;
using Microsoft.Azure.Cosmos;

namespace MapMe.Repositories;

/// <summary>
/// CosmosDB implementation of user profile repository
/// </summary>
public sealed class CosmosUserProfileRepository : CosmosRepositoryBase<UserProfile>, IUserProfileRepository
{
    private const string ContainerName = "UserProfiles";
    private const string PartitionKeyPath = "/id";

    public CosmosUserProfileRepository(CosmosClient cosmosClient, CosmosContextOptions options)
        : base(cosmosClient, options, ContainerName)
    {
        // Ensure container exists on startup
        _ = Task.Run(async () => await EnsureContainerExistsAsync(PartitionKeyPath, 400));
    }

    /// <summary>
    /// Gets a user profile by its unique ID
    /// </summary>
    public async Task<UserProfile?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return await GetItemAsync(id, id, ct);
    }

    /// <summary>
    /// Gets a user profile by user ID (may be different from document ID)
    /// </summary>
    public async Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var queryDefinition = new QueryDefinition(
            "SELECT * FROM c WHERE c.userId = @userId")
            .WithParameter("@userId", userId);

        var results = await QueryItemsAsync(queryDefinition, ct);
        return results.Count > 0 ? results[0] : null;
    }

    /// <summary>
    /// Creates or updates a user profile
    /// </summary>
    public async Task UpsertAsync(UserProfile profile, CancellationToken ct = default)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        await UpsertItemAsync(profile, profile.Id, ct);
    }

    /// <summary>
    /// Deletes a user profile by ID
    /// </summary>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        await DeleteItemAsync(id, id, ct);
    }
}
