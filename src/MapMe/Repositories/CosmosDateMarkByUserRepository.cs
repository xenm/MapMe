using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using MapMe.Data;
using MapMe.Models;
using Microsoft.Azure.Cosmos;

namespace MapMe.Repositories;

/// <summary>
/// CosmosDB implementation of DateMark repository with geospatial query support
/// </summary>
public sealed class CosmosDateMarkByUserRepository : CosmosRepositoryBase<DateMark>, IDateMarkByUserRepository
{
    private const string ContainerName = "DateMarks";
    private const string PartitionKeyPath = "/userId";

    public CosmosDateMarkByUserRepository(CosmosClient cosmosClient, CosmosContextOptions options)
        : base(cosmosClient, options, ContainerName)
    {
        // Ensure container exists on startup with geospatial indexing
        _ = Task.Run(async () => await EnsureContainerWithGeospatialIndexAsync());
    }

    /// <summary>
    /// Creates or updates a DateMark
    /// </summary>
    public async Task UpsertAsync(DateMark mark, CancellationToken ct = default)
    {
        if (mark == null)
            throw new ArgumentNullException(nameof(mark));

        await UpsertItemAsync(mark, mark.UserId, ct);
    }

    /// <summary>
    /// Gets a DateMark by user ID and DateMark ID
    /// </summary>
    public async Task<DateMark?> GetByIdAsync(string userId, string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(id))
            return null;

        return await GetItemAsync(id, userId, ct);
    }

    /// <summary>
    /// Gets DateMarks for a user with optional filtering
    /// </summary>
    public async IAsyncEnumerable<DateMark> GetByUserAsync(
        string userId,
        DateOnly? from = null,
        DateOnly? to = null,
        IReadOnlyCollection<string>? categories = null,
        IReadOnlyCollection<string>? tags = null,
        IReadOnlyCollection<string>? qualities = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            yield break;

        // Build dynamic query based on filters
        var sql = "SELECT * FROM c WHERE c.userId = @userId AND (NOT IS_DEFINED(c.isDeleted) OR c.isDeleted = false)";
        var queryDefinition = new QueryDefinition(sql).WithParameter("@userId", userId);

        // Add date range filters to SQL for better performance
        if (from.HasValue)
        {
            sql += " AND c.visitDate >= @fromDate";
            queryDefinition = queryDefinition.WithParameter("@fromDate", from.Value.ToString("yyyy-MM-dd"));
        }

        if (to.HasValue)
        {
            sql += " AND c.visitDate <= @toDate";
            queryDefinition = queryDefinition.WithParameter("@toDate", to.Value.ToString("yyyy-MM-dd"));
        }

        // Add array filters for categories, tags, qualities
        if (categories is { Count: > 0 })
        {
            sql += " AND ARRAY_LENGTH(ARRAY(SELECT VALUE c FROM c IN c.categoriesNorm WHERE c IN (@categories))) > 0";
            queryDefinition = queryDefinition.WithParameter("@categories", categories.ToArray());
        }

        if (tags is { Count: > 0 })
        {
            sql += " AND ARRAY_LENGTH(ARRAY(SELECT VALUE t FROM t IN c.tagsNorm WHERE t IN (@tags))) > 0";
            queryDefinition = queryDefinition.WithParameter("@tags", tags.ToArray());
        }

        if (qualities is { Count: > 0 })
        {
            sql += " AND ARRAY_LENGTH(ARRAY(SELECT VALUE q FROM q IN c.qualitiesNorm WHERE q IN (@qualities))) > 0";
            queryDefinition = queryDefinition.WithParameter("@qualities", qualities.ToArray());
        }

        queryDefinition = new QueryDefinition(sql);
        queryDefinition = queryDefinition.WithParameter("@userId", userId);

        if (from.HasValue)
            queryDefinition = queryDefinition.WithParameter("@fromDate", from.Value.ToString("yyyy-MM-dd"));
        if (to.HasValue)
            queryDefinition = queryDefinition.WithParameter("@toDate", to.Value.ToString("yyyy-MM-dd"));
        if (categories is { Count: > 0 })
            queryDefinition = queryDefinition.WithParameter("@categories", categories.ToArray());
        if (tags is { Count: > 0 })
            queryDefinition = queryDefinition.WithParameter("@tags", tags.ToArray());
        if (qualities is { Count: > 0 })
            queryDefinition = queryDefinition.WithParameter("@qualities", qualities.ToArray());

        await foreach (var item in QueryItemsAsyncEnumerable(queryDefinition, ct))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Ensures the container exists with proper geospatial indexing for location queries
    /// </summary>
    private async Task EnsureContainerWithGeospatialIndexAsync()
    {
        var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_options.DatabaseName);

        var containerProperties = new ContainerProperties(ContainerName, PartitionKeyPath);

        // Configure indexing policy for geospatial queries
        containerProperties.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
        containerProperties.IndexingPolicy.Automatic = true;

        // Add geospatial index for location-based queries
        containerProperties.IndexingPolicy.SpatialIndexes.Add(new SpatialPath
        {
            Path = "/location/*",
            SpatialTypes = { SpatialType.Point }
        });

        // Add composite indexes for common query patterns
        containerProperties.IndexingPolicy.CompositeIndexes.Add(new Collection<CompositePath>
        {
            new CompositePath { Path = "/userId", Order = CompositePathSortOrder.Ascending },
            new CompositePath { Path = "/visitDate", Order = CompositePathSortOrder.Descending }
        });

        await database.Database.CreateContainerIfNotExistsAsync(containerProperties, 400);
    }

    /// <summary>
    /// Deletes a DateMark
    /// </summary>
    public async Task DeleteAsync(string userId, string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(id))
            return;

        await DeleteItemAsync(id, userId, ct);
    }

    /// <summary>
    /// Gets DateMarks within a geographic radius (for map viewport queries)
    /// </summary>
    public async IAsyncEnumerable<DateMark> GetByLocationAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        IReadOnlyCollection<string>? categories = null,
        IReadOnlyCollection<string>? tags = null,
        IReadOnlyCollection<string>? qualities = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Use ST_DISTANCE for geospatial queries
        var sql = @"SELECT * FROM c 
                   WHERE ST_DISTANCE(c.location, {'type': 'Point', 'coordinates': [@lng, @lat]}) <= @radius
                   AND (NOT IS_DEFINED(c.isDeleted) OR c.isDeleted = false)";

        var queryDefinition = new QueryDefinition(sql)
            .WithParameter("@lat", latitude)
            .WithParameter("@lng", longitude)
            .WithParameter("@radius", radiusMeters);

        // Add category/tag/quality filters
        if (categories is { Count: > 0 })
        {
            sql += " AND ARRAY_LENGTH(ARRAY(SELECT VALUE c FROM c IN c.categoriesNorm WHERE c IN (@categories))) > 0";
            queryDefinition = queryDefinition.WithParameter("@categories", categories.ToArray());
        }

        if (tags is { Count: > 0 })
        {
            sql += " AND ARRAY_LENGTH(ARRAY(SELECT VALUE t FROM t IN c.tagsNorm WHERE t IN (@tags))) > 0";
            queryDefinition = queryDefinition.WithParameter("@tags", tags.ToArray());
        }

        if (qualities is { Count: > 0 })
        {
            sql += " AND ARRAY_LENGTH(ARRAY(SELECT VALUE q FROM q IN c.qualitiesNorm WHERE q IN (@qualities))) > 0";
            queryDefinition = queryDefinition.WithParameter("@qualities", qualities.ToArray());
        }

        queryDefinition = new QueryDefinition(sql)
            .WithParameter("@lat", latitude)
            .WithParameter("@lng", longitude)
            .WithParameter("@radius", radiusMeters);

        if (categories is { Count: > 0 })
            queryDefinition = queryDefinition.WithParameter("@categories", categories.ToArray());
        if (tags is { Count: > 0 })
            queryDefinition = queryDefinition.WithParameter("@tags", tags.ToArray());
        if (qualities is { Count: > 0 })
            queryDefinition = queryDefinition.WithParameter("@qualities", qualities.ToArray());

        await foreach (var item in QueryItemsAsyncEnumerable(queryDefinition, ct))
        {
            yield return item;
        }
    }
}