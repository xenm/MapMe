# SystemTextJsonCosmosSerializer

This document describes the custom Cosmos DB serializer implementation that uses System.Text.Json instead of Newtonsoft.Json, ensuring consistent JSON serialization throughout the MapMe application.

## Overview

The `SystemTextJsonCosmosSerializer` class is a custom implementation of the `CosmosSerializer` base class that eliminates the dependency on Newtonsoft.Json while providing full compatibility with Azure Cosmos DB operations.

## Benefits

### Security
- **Eliminates Vulnerable Dependencies**: Removes high-severity security vulnerabilities from Newtonsoft.Json 10.0.2
- **No Transitive Dependencies**: Clean dependency tree without legacy JSON libraries

### Performance
- **Memory Efficiency**: System.Text.Json uses less memory than Newtonsoft.Json
- **Faster Serialization**: Better performance for JSON operations
- **Reduced Allocations**: More efficient object allocation patterns

### Consistency
- **Single JSON Library**: Entire application uses System.Text.Json exclusively
- **Unified Configuration**: Same serialization options across client and server
- **Predictable Behavior**: Consistent JSON handling throughout the stack

## Implementation

### Class Structure

```csharp
public class SystemTextJsonCosmosSerializer : CosmosSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly JsonSerializerOptions _options;

    public SystemTextJsonCosmosSerializer() : this(DefaultOptions) { }
    
    public SystemTextJsonCosmosSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
}
```

### Key Methods

#### FromStream<T>(Stream stream)
Deserializes a stream containing JSON data to an object of type T.

**Features:**
- Null stream handling
- Empty stream detection
- Stream type passthrough for raw data
- Comprehensive error handling

```csharp
public override T FromStream<T>(Stream stream)
{
    if (stream == null)
        throw new ArgumentNullException(nameof(stream));

    if (stream.CanSeek && stream.Length == 0)
        return default!;

    if (typeof(Stream).IsAssignableFrom(typeof(T)))
        return (T)(object)stream;

    using var streamReader = new StreamReader(stream);
    var json = streamReader.ReadToEnd();
    
    if (string.IsNullOrEmpty(json))
        return default!;

    return JsonSerializer.Deserialize<T>(json, _options)!;
}
```

#### ToStream<T>(T input)
Serializes an object to a stream containing JSON data.

**Features:**
- Null input handling
- Stream type passthrough
- Memory-efficient stream creation
- Proper stream positioning

```csharp
public override Stream ToStream<T>(T input)
{
    if (input == null)
        return new MemoryStream();

    if (input is Stream inputStream)
        return inputStream;

    var json = JsonSerializer.Serialize(input, _options);
    var stream = new MemoryStream();
    var writer = new StreamWriter(stream);
    writer.Write(json);
    writer.Flush();
    stream.Position = 0;
    return stream;
}
```

## Configuration

### Default JSON Options

The serializer uses optimized JSON serialization options:

```csharp
private static readonly JsonSerializerOptions DefaultOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,        // camelCase property names
    PropertyNameCaseInsensitive = true,                       // Accept any case on deserialization
    WriteIndented = false,                                    // Compact JSON for performance
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull  // Skip null values
};
```

### Cosmos Client Integration

The serializer is configured in the Cosmos client setup:

```csharp
// Program.cs
var options = new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway,
    Serializer = new SystemTextJsonCosmosSerializer()  // Custom serializer
};

if (isLocal)
{
    options.HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
}

return new CosmosClient(cosmosEndpoint!, cosmosKey!, options);
```

### Project Configuration

The project file is configured to bypass Newtonsoft.Json requirements:

```xml
<PropertyGroup>
    <!-- Disable Cosmos DB Newtonsoft.Json requirement - we use System.Text.Json -->
    <AzureCosmosDisableNewtonsoftJsonCheck>true</AzureCosmosDisableNewtonsoftJsonCheck>
</PropertyGroup>
```

## Usage Examples

### Custom Serializer Options

You can create a custom serializer with different options:

```csharp
var customOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never
};

var customSerializer = new SystemTextJsonCosmosSerializer(customOptions);

var cosmosOptions = new CosmosClientOptions
{
    Serializer = customSerializer
};
```

### Repository Integration

The serializer works transparently with all Cosmos repositories:

```csharp
// CosmosUserProfileRepository automatically uses the custom serializer
var userProfile = new UserProfile
{
    UserId = "user-123",
    DisplayName = "John Doe",
    Bio = "Love exploring new places!"
};

await repository.UpsertAsync(userProfile);  // Uses SystemTextJsonCosmosSerializer
var retrieved = await repository.GetByIdAsync("user-123");  // Also uses custom serializer
```

## Error Handling

The serializer includes comprehensive error handling:

### Stream Validation
- Null stream arguments throw `ArgumentNullException`
- Empty streams return default values
- Invalid JSON content is handled gracefully

### Serialization Errors
- Serialization exceptions are propagated with context
- Type conversion errors are handled appropriately
- Memory streams are properly disposed

### Performance Considerations
- Streams are positioned correctly for reading
- Memory usage is optimized for large objects
- No unnecessary object allocations

## Testing

The serializer is tested through the existing Cosmos repository tests:

```bash
# Run all tests including Cosmos integration
dotnet test MapMe/MapMe.Tests

# Specific repository tests
dotnet test --filter "CosmosUserProfileRepository"
dotnet test --filter "CosmosDateMarkByUserRepository"
```

## Migration Notes

### From Newtonsoft.Json

The migration from Newtonsoft.Json to System.Text.Json is transparent:

1. **No Code Changes**: Existing repository code works without modification
2. **Same Functionality**: All Cosmos operations continue to work
3. **Better Performance**: Improved memory usage and serialization speed
4. **Enhanced Security**: Eliminates vulnerable dependencies

### Compatibility

The serializer maintains full compatibility with:
- All existing Cosmos DB operations (CRUD, queries, transactions)
- Geospatial data types and queries
- Complex nested objects and collections
- Custom data types and converters

## Troubleshooting

### Common Issues

**Serialization Errors:**
- Ensure all model properties have public getters/setters
- Check for circular references in object graphs
- Verify custom converters are compatible with System.Text.Json

**Performance Issues:**
- Monitor memory usage with large objects
- Consider custom JsonSerializerOptions for specific use cases
- Use streaming APIs for very large datasets

**Compatibility Problems:**
- Verify all data types are supported by System.Text.Json
- Check date/time formatting matches expectations
- Ensure enum serialization works as expected

### Debugging

Enable detailed logging to troubleshoot serialization issues:

```csharp
// Add logging to see serialization details
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true  // Enable for debugging
};
```

## Future Enhancements

### Planned Improvements
- **Custom Converters**: Add support for specialized data types
- **Performance Monitoring**: Add metrics for serialization operations
- **Configuration Options**: Make serializer options configurable via appsettings
- **Compression**: Add optional compression for large documents

### Advanced Features
- **Schema Validation**: Optional JSON schema validation
- **Versioning Support**: Handle model versioning and migration
- **Caching**: Cache serialized representations for performance
- **Batch Optimization**: Optimize for bulk operations

## References

- [System.Text.Json Documentation](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [Azure Cosmos DB Custom Serialization](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/custom-serialization)
- [CosmosSerializer Base Class](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosserializer)
- [.NET 10 JSON Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to)
