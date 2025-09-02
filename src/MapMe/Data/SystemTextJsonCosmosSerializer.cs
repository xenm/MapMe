using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;

namespace MapMe.Data;

/// <summary>
/// Custom Cosmos DB serializer that uses System.Text.Json instead of Newtonsoft.Json
/// This ensures consistent JSON serialization throughout the application following .NET 10 best practices
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of SystemTextJsonCosmosSerializer with default options
    /// </summary>
    public SystemTextJsonCosmosSerializer() : this(DefaultOptions)
    {
    }

    /// <summary>
    /// Initializes a new instance of SystemTextJsonCosmosSerializer with custom options
    /// </summary>
    /// <param name="options">Custom JsonSerializerOptions to use for serialization</param>
    public SystemTextJsonCosmosSerializer(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Deserializes a stream to an object of type T
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="stream">The stream containing JSON data</param>
    /// <returns>Deserialized object of type T</returns>
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

    /// <summary>
    /// Serializes an object to a stream
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="input">The object to serialize</param>
    /// <returns>Stream containing serialized JSON data</returns>
    public override Stream ToStream<T>(T input)
    {
        if (EqualityComparer<T>.Default.Equals(input, default))
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
}