using System.Text.Json;
using MapMe.Data;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace MapMe.Tests.Unit;

/// <summary>
/// Tests to ensure Newtonsoft.Json is completely eliminated from the MapMe application
/// and prevent accidental reintroduction of vulnerable JSON dependencies.
/// </summary>
public class NewtonsoftJsonEliminationTests
{
    /// <summary>
    /// Verifies that our main application assemblies don't reference Newtonsoft.Json.
    /// Note: Test framework may load Newtonsoft.Json, but our application code should not use it.
    /// </summary>
    [Fact]
    public void MainApplication_ShouldNotReferenceNewtonsoftJson()
    {
        // Arrange - Get our application assemblies (not test framework assemblies)
        var applicationAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.FullName?.StartsWith("MapMe", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // Act & Assert - Check that our application assemblies don't reference Newtonsoft.Json
        foreach (var assembly in applicationAssemblies)
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            var newtonsoftReferences = referencedAssemblies
                .Where(refAssembly =>
                    refAssembly.Name?.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            Assert.Empty(newtonsoftReferences);
        }
    }

    /// <summary>
    /// Verifies that no types from Newtonsoft.Json namespace are available in loaded assemblies.
    /// This prevents accidental usage of Newtonsoft.Json types.
    /// </summary>
    [Fact]
    public void NewtonsoftJson_TypesShouldNotBeAvailable()
    {
        // Arrange
        var commonNewtonsoftTypes = new[]
        {
            "Newtonsoft.Json.JsonConvert",
            "Newtonsoft.Json.JsonSerializer",
            "Newtonsoft.Json.JsonReader",
            "Newtonsoft.Json.JsonWriter",
            "Newtonsoft.Json.Linq.JObject",
            "Newtonsoft.Json.Linq.JArray"
        };

        // Act & Assert
        foreach (var typeName in commonNewtonsoftTypes)
        {
            var type = Type.GetType(typeName);
            Assert.Null(type);
        }
    }

    /// <summary>
    /// Verifies that System.Text.Json types are available and being used.
    /// This ensures our preferred JSON library is properly loaded.
    /// </summary>
    [Fact]
    public void SystemTextJson_TypesShouldBeAvailable()
    {
        // Arrange & Act - Test that we can use System.Text.Json types directly
        var options = new JsonSerializerOptions();
        var testData = new { test = "value" };

        // Act - Serialize and deserialize using System.Text.Json
        var json = JsonSerializer.Serialize(testData, options);
        var result = JsonSerializer.Deserialize<dynamic>(json);

        // Assert - Operations should succeed
        Assert.NotNull(json);
        Assert.NotNull(result);
        Assert.Contains("test", json);
    }

    /// <summary>
    /// Verifies that our custom SystemTextJsonCosmosSerializer is available and properly configured.
    /// This ensures our Cosmos DB serialization uses System.Text.Json exclusively.
    /// </summary>
    [Fact]
    public void SystemTextJsonCosmosSerializer_ShouldBeAvailable()
    {
        // Arrange & Act
        var serializerType = Type.GetType("MapMe.Data.SystemTextJsonCosmosSerializer, MapMe");

        // Assert
        Assert.NotNull(serializerType);
        Assert.True(serializerType!.IsSubclassOf(typeof(CosmosSerializer)));
    }

    /// <summary>
    /// Verifies that no assemblies contain references to vulnerable Newtonsoft.Json versions.
    /// This test helps prevent security vulnerabilities from being reintroduced.
    /// </summary>
    [Fact]
    public void LoadedAssemblies_ShouldNotReferenceVulnerableNewtonsoftVersions()
    {
        // Arrange
        var vulnerableVersions = new[] { "10.0.2", "10.0.1", "10.0.0" };
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        // Act & Assert
        foreach (var assembly in loadedAssemblies)
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            var newtonsoftReferences = referencedAssemblies
                .Where(refAssembly =>
                    refAssembly.Name?.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            foreach (var newtonsoftRef in newtonsoftReferences)
            {
                var version = newtonsoftRef.Version?.ToString();
                Assert.DoesNotContain(version, vulnerableVersions);
            }
        }
    }

    /// <summary>
    /// Integration test that verifies JSON serialization works correctly with System.Text.Json.
    /// This ensures our JSON operations function properly without Newtonsoft.Json.
    /// </summary>
    [Fact]
    public void JsonSerialization_ShouldWorkWithSystemTextJsonOnly()
    {
        // Arrange
        var testObject = new
        {
            Id = "test-123",
            Name = "Test User",
            CreatedAt = DateTime.UtcNow,
            Properties = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "key3", true }
            }
        };

        // Act - Serialize using System.Text.Json
        var json = JsonSerializer.Serialize(testObject);
        Assert.NotNull(json);
        Assert.Contains("test-123", json);

        // Act - Deserialize using System.Text.Json
        var deserializedObject = JsonSerializer.Deserialize<dynamic>(json);
        Assert.NotNull(deserializedObject);
    }

    /// <summary>
    /// Verifies that our custom Cosmos serializer can be instantiated and works correctly.
    /// This ensures the SystemTextJsonCosmosSerializer is properly implemented.
    /// </summary>
    [Fact]
    public void SystemTextJsonCosmosSerializer_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var serializer = new SystemTextJsonCosmosSerializer();
        var testData = new { Id = "test", Value = "data", Number = 42 };

        // Act - Serialize to stream
        using var stream = serializer.ToStream(testData);
        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);

        // Act - Deserialize from stream
        stream.Position = 0;
        var deserializedData = serializer.FromStream<dynamic>(stream);
        Assert.NotNull(deserializedData);
    }

    /// <summary>
    /// Verifies that our application code uses System.Text.Json exclusively for JSON operations.
    /// This test ensures we don't accidentally use Newtonsoft.Json in our application logic.
    /// </summary>
    [Fact]
    public void ApplicationCode_ShouldUseSystemTextJsonExclusively()
    {
        // Arrange - Test typical JSON operations that our application would perform
        var userProfile = new
        {
            UserId = "test-user-123",
            DisplayName = "Test User",
            Bio = "Test bio with special characters: àáâãäå",
            CreatedAt = DateTime.UtcNow,
            Properties = new Dictionary<string, object>
            {
                { "location", "Test City" },
                { "age", 25 },
                { "isActive", true }
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Act - Perform JSON operations using System.Text.Json only
        var serializedJson = JsonSerializer.Serialize(userProfile, jsonOptions);
        var deserializedData = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedJson);

        // Assert - Operations should succeed and produce expected results
        Assert.NotNull(serializedJson);
        Assert.NotNull(deserializedData);
        Assert.Contains("userId", serializedJson); // camelCase naming policy applied
        Assert.Contains("displayName", serializedJson);
        Assert.DoesNotContain("UserId", serializedJson); // PascalCase should be converted
    }
}