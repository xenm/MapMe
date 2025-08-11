namespace MapMe.Repositories;

public sealed class CosmosContextOptions
{
    public string DatabaseName { get; }
    public CosmosContextOptions(string databaseName) => DatabaseName = databaseName;
}
