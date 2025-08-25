using System;

namespace MapMe.Data;

/// <summary>
/// Configuration options for CosmosDB context
/// </summary>
/// <param name="DatabaseName">The name of the CosmosDB database</param>
public record CosmosContextOptions(string DatabaseName);
