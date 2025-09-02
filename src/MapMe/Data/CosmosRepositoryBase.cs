using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;

namespace MapMe.Data;

/// <summary>
/// Base class for CosmosDB repositories providing common functionality
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class CosmosRepositoryBase<T> where T : class
{
    protected readonly Container _container;
    protected readonly CosmosClient _cosmosClient;
    protected readonly CosmosContextOptions _options;

    protected CosmosRepositoryBase(CosmosClient cosmosClient, CosmosContextOptions options, string containerName)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var database = _cosmosClient.GetDatabase(_options.DatabaseName);
        _container = database.GetContainer(containerName);
    }

    /// <summary>
    /// Ensures the database and container exist with proper configuration
    /// </summary>
    protected async Task EnsureContainerExistsAsync(string partitionKeyPath, int? throughput = null)
    {
        var database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_options.DatabaseName);

        var containerProperties = new ContainerProperties(_container.Id, partitionKeyPath);

        // Configure indexing policy for better performance
        containerProperties.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
        containerProperties.IndexingPolicy.Automatic = true;

        await database.Database.CreateContainerIfNotExistsAsync(
            containerProperties,
            throughput);
    }

    /// <summary>
    /// Gets an item by ID and partition key
    /// </summary>
    protected async Task<T?> GetItemAsync<TPartitionKey>(string id, TPartitionKey partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey?.ToString()),
                cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Upserts an item to the container
    /// </summary>
    protected async Task<T> UpsertItemAsync<TPartitionKey>(T item, TPartitionKey partitionKey,
        CancellationToken cancellationToken = default)
    {
        var response = await _container.UpsertItemAsync(item, new PartitionKey(partitionKey?.ToString()),
            cancellationToken: cancellationToken);
        return response.Resource;
    }

    /// <summary>
    /// Deletes an item from the container
    /// </summary>
    protected async Task DeleteItemAsync<TPartitionKey>(string id, TPartitionKey partitionKey,
        CancellationToken cancellationToken = default)
    {
        await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey?.ToString()),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes a query and returns all results
    /// </summary>
    protected async Task<List<T>> QueryItemsAsync(QueryDefinition queryDefinition,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();

        using var feedIterator = _container.GetItemQueryIterator<T>(queryDefinition);

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    /// <summary>
    /// Executes a query and returns results as an async enumerable
    /// </summary>
    protected async IAsyncEnumerable<T> QueryItemsAsyncEnumerable(QueryDefinition queryDefinition,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var feedIterator = _container.GetItemQueryIterator<T>(queryDefinition);

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            foreach (var item in response)
            {
                yield return item;
            }
        }
    }
}