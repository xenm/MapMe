using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using MapMe.Services;
using Xunit;

namespace MapMe.Tests.Integration;

/// <summary>
/// Integration tests focused on error handling, malformed requests, and boundary conditions
/// for the MapMe API endpoints.
/// </summary>
[Trait("Category", "Integration")]
public class ErrorHandlingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ErrorHandlingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove any existing Cosmos DB registrations
                var cosmosDescriptors = services.Where(d => 
                    d.ServiceType == typeof(IUserProfileRepository) ||
                    d.ServiceType == typeof(IDateMarkByUserRepository))
                    .ToList();
                
                foreach (var descriptor in cosmosDescriptors)
                {
                    services.Remove(descriptor);
                }
                
                // Register in-memory implementations for testing
                services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
                services.AddSingleton<IDateMarkByUserRepository, InMemoryDateMarkByUserRepository>();
                
                // Override authentication service for testing
                var authDescriptors = services.Where(d => d.ServiceType == typeof(IAuthenticationService)).ToList();
                foreach (var descriptor in authDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<IAuthenticationService, TestAuthenticationService>();
            });
        });
        
        _client = _factory.CreateClient();
    }

    #region Malformed Request Tests

    [Fact]
    public async Task Profile_Create_WithMalformedJson_ReturnsBadRequest()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Test with invalid JSON
        var malformedJsons = new[]
        {
            "{", // Incomplete JSON
            "{ \"Id\": \"test\", }", // Trailing comma
            "{ \"Id\": \"test\", \"UserId\": }", // Missing value
            "not json at all", // Not JSON
            "", // Empty string
            "null" // Null JSON
        };

        foreach (var json in malformedJsons)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/profiles", content);
            
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task DateMark_Create_WithMalformedJson_ReturnsBadRequest()
    {
        // Test with invalid JSON for DateMark creation
        var malformedJsons = new[]
        {
            "{ \"Id\": \"test\", \"Latitude\": \"not a number\" }", // Invalid number
            "{ \"Id\": \"test\", \"Latitude\": 37.0, \"Longitude\": }", // Missing value
            "{ \"Id\": \"test\", \"Categories\": \"not an array\" }", // Wrong type
        };

        foreach (var json in malformedJsons)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/datemarks", content);
            
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Profile_Create_WithExcessivelyLongStrings_HandlesGracefully()
    {
        // Test with very long strings that might cause issues
        var veryLongString = new string('A', 100000); // 100KB string
        
        var request = new CreateProfileRequest(
            Id: "long_string_test",
            UserId: "long_user",
            DisplayName: veryLongString,
            Bio: veryLongString,
            Photos: Array.Empty<UserPhoto>(),
            PreferredCategories: Array.Empty<string>(),
            Visibility: "public"
        );

        var response = await _client.PostAsJsonAsync("/api/profiles", request);
        
        // Should either succeed or fail gracefully with appropriate status code
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created, 
            HttpStatusCode.BadRequest, 
            HttpStatusCode.RequestEntityTooLarge
        );
    }

    #endregion

    #region HTTP Method Tests

    [Fact]
    public async Task Profile_UnsupportedHttpMethods_ReturnMethodNotAllowed()
    {
        // Test unsupported HTTP methods on profile endpoints
        var methods = new[]
        {
            HttpMethod.Put,
            HttpMethod.Delete,
            HttpMethod.Patch
        };

        foreach (var method in methods)
        {
            var request = new HttpRequestMessage(method, "/api/profiles");
            var response = await _client.SendAsync(request);
            
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }
    }

    [Fact]
    public async Task DateMark_UnsupportedHttpMethods_ReturnMethodNotAllowed()
    {
        // Test unsupported HTTP methods on DateMark endpoints
        var methods = new[]
        {
            HttpMethod.Put,
            HttpMethod.Delete,
            HttpMethod.Patch
        };

        foreach (var method in methods)
        {
            var request = new HttpRequestMessage(method, "/api/datemarks");
            var response = await _client.SendAsync(request);
            
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }
    }

    #endregion

    #region Content Type Tests

    [Fact]
    public async Task Profile_Create_WithWrongContentType_ReturnsBadRequest()
    {
        // Test with wrong content types
        var validJson = JsonSerializer.Serialize(new CreateProfileRequest(
            "test", "user", "Name", null, null, null, "public"));

        var wrongContentTypes = new[]
        {
            "text/plain",
            "application/xml",
            "application/x-www-form-urlencoded",
            "multipart/form-data"
        };

        foreach (var contentType in wrongContentTypes)
        {
            var content = new StringContent(validJson, Encoding.UTF8, contentType);
            var response = await _client.PostAsync("/api/profiles", content);
            
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.UnsupportedMediaType
            );
        }
    }

    #endregion

    #region Query Parameter Edge Cases

    [Fact]
    public async Task DateMark_Query_WithInvalidQueryParameters_HandlesGracefully()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Create a test user and DateMark first
        var userId = "test_user_id"; // Match TestAuthenticationService
        var request = new UpsertDateMarkRequest(
            Id: "query_param_test",
            UserId: userId,
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: null,
            PlaceName: "Test Place",
            PlaceTypes: null,
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "Restaurant" },
            Tags: new[] { "Casual" },
            Qualities: new[] { "Good Food" },
            Notes: "Test note",
            VisitDate: new DateOnly(2025, 8, 12),
            Visibility: "public"
        );

        await _client.PostAsJsonAsync("/api/datemarks", request);

        // Test with invalid query parameters
        var invalidQueries = new[]
        {
            $"/api/users/{userId}/datemarks?from=invalid-date",
            $"/api/users/{userId}/datemarks?to=not-a-date",
            $"/api/users/{userId}/datemarks?from=2025-13-01", // Invalid month
            $"/api/users/{userId}/datemarks?from=2025-02-30", // Invalid day
            $"/api/users/{userId}/datemarks?categories=", // Empty parameter
            $"/api/users/{userId}/datemarks?tags=&qualities=", // Multiple empty parameters
        };

        foreach (var query in invalidQueries)
        {
            var response = await _client.GetAsync(query);
            
            // Should handle invalid parameters gracefully
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK, // Ignores invalid parameters
                HttpStatusCode.BadRequest // Validates and rejects
            );
        }
    }

    [Fact]
    public async Task DateMark_Query_WithExtremelyLongQueryString_HandlesGracefully()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        var userId = "test_user_id"; // Match TestAuthenticationService
        
        // Create a very long query string with many categories
        var categories = string.Join(",", Enumerable.Range(0, 1000).Select(i => $"category_{i}"));
        var longQuery = $"/api/users/{userId}/datemarks?categories={categories}";
        
        var response = await _client.GetAsync(longQuery);
        
        // Should handle long query strings gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestUriTooLong
        );
    }

    #endregion

    #region Special Characters and Encoding Tests

    [Fact]
    public async Task Profile_Create_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Test with various special characters and encodings
        var specialCharacterTests = new[]
        {
            ("emoji_test", "User with Emojis 🎉🚀💖", "Bio with emojis 🌟✨🎊"),
            ("unicode_test", "Üñíçødé Tëst Üsér", "Bio with üñíçødé çhäräçtërs"),
            ("chinese_test", "中文用户", "这是中文简介"),
            ("arabic_test", "مستخدم عربي", "هذه سيرة ذاتية باللغة العربية"),
            ("russian_test", "Русский пользователь", "Это русская биография"),
            ("symbols_test", "User@#$%^&*()", "Bio with symbols: !@#$%^&*()_+-=[]{}|;':\",./<>?"),
            ("quotes_test", "User \"with\" 'quotes'", "Bio with \"double\" and 'single' quotes"),
            ("newlines_test", "User\nWith\nNewlines", "Bio\nwith\nmultiple\nlines"),
            ("tabs_test", "User\tWith\tTabs", "Bio\twith\ttab\tcharacters")
        };

        foreach (var (id, displayName, bio) in specialCharacterTests)
        {
            var request = new CreateProfileRequest(
                Id: id,
                UserId: "test_user_id", // Match TestAuthenticationService
                DisplayName: displayName,
                Bio: bio,
                Photos: Array.Empty<UserPhoto>(),
                PreferredCategories: Array.Empty<string>(),
                Visibility: "public"
            );

            var response = await _client.PostAsJsonAsync("/api/profiles", request);
            response.EnsureSuccessStatusCode();

            // Verify the data was stored correctly
            var getResponse = await _client.GetAsync($"/api/profiles/{id}");
            getResponse.EnsureSuccessStatusCode();
            
            var profile = await getResponse.Content.ReadFromJsonAsync<UserProfile>();
            profile.Should().NotBeNull();
            profile!.DisplayName.Should().Be(displayName);
            profile.Bio.Should().Be(bio);
        }
    }

    [Fact]
    public async Task DateMark_Create_WithSpecialCharactersInArrays_HandlesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Test special characters in array fields
        var request = new UpsertDateMarkRequest(
            Id: "special_arrays_test",
            UserId: "test_user_id", // Match TestAuthenticationService
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: null,
            PlaceName: "Café Münchën 🍕",
            PlaceTypes: new[] { "restaurant", "café" },
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: "123 Münchën Straße, São Paulo",
            City: "São Paulo",
            Country: "Brasil",
            Categories: new[] { "Fine Dining", "Café & Bistro", "Français" },
            Tags: new[] { "Romantique", "Cher", "Spécial" },
            Qualities: new[] { "Excellente Nourriture", "Service Rapide", "Ambiance Cozy" },
            Notes: "Un endroit magnifique avec des caractères spéciaux! 🌟",
            VisitDate: new DateOnly(2025, 8, 12),
            Visibility: "public"
        );

        var response = await _client.PostAsJsonAsync("/api/datemarks", request);
        response.EnsureSuccessStatusCode();

        // Verify the data was stored correctly
        var listResponse = await _client.GetAsync("/api/users/test_user_id/datemarks");
        listResponse.EnsureSuccessStatusCode();
        
        var dateMarks = await listResponse.Content.ReadFromJsonAsync<List<DateMark>>();
        dateMarks.Should().HaveCount(1);
        
        var dateMark = dateMarks!.First();
        dateMark.PlaceSnapshot!.Name.Should().Be("Café Münchën 🍕");
        dateMark.Categories.Should().Contain("Fine Dining");
        dateMark.Tags.Should().Contain("Romantique");
        dateMark.Qualities.Should().Contain("Excellente Nourriture");
        dateMark.Notes.Should().Be("Un endroit magnifique avec des caractères spéciaux! 🌟");
    }

    #endregion

    #region Large Data Tests

    [Fact]
    public async Task Profile_Create_WithManyPhotos_HandlesCorrectly()
    {
        // Test with a large number of photos
        var photos = new List<UserPhoto>();
        for (int i = 0; i < 100; i++)
        {
            photos.Add(new UserPhoto($"https://example.com/photo_{i}.jpg", i == 0));
        }

        var request = new CreateProfileRequest(
            Id: "many_photos_test",
            UserId: "many_photos_user",
            DisplayName: "User with Many Photos",
            Bio: "Testing with many photos",
            Photos: photos,
            PreferredCategories: Array.Empty<string>(),
            Visibility: "public"
        );

        var response = await _client.PostAsJsonAsync("/api/profiles", request);
        
        // Should handle large photo arrays gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge
        );

        if (response.IsSuccessStatusCode)
        {
            var getResponse = await _client.GetAsync("/api/profiles/many_photos_test");
            getResponse.EnsureSuccessStatusCode();
            
            var profile = await getResponse.Content.ReadFromJsonAsync<UserProfile>();
            profile.Should().NotBeNull();
            profile!.Photos.Should().HaveCount(100);
        }
    }

    [Fact]
    public async Task DateMark_Create_WithManyCategories_HandlesCorrectly()
    {
        // Test with a large number of categories, tags, and qualities
        var categories = Enumerable.Range(0, 50).Select(i => $"Category_{i}").ToArray();
        var tags = Enumerable.Range(0, 50).Select(i => $"Tag_{i}").ToArray();
        var qualities = Enumerable.Range(0, 50).Select(i => $"Quality_{i}").ToArray();

        var request = new UpsertDateMarkRequest(
            Id: "many_arrays_test",
            UserId: "many_arrays_user",
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: null,
            PlaceName: "Place with Many Arrays",
            PlaceTypes: null,
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: categories,
            Tags: tags,
            Qualities: qualities,
            Notes: "Testing with many array elements",
            VisitDate: new DateOnly(2025, 8, 12),
            Visibility: "public"
        );

        var response = await _client.PostAsJsonAsync("/api/datemarks", request);
        
        // Should handle large arrays gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.BadRequest,
            HttpStatusCode.RequestEntityTooLarge
        );

        if (response.IsSuccessStatusCode)
        {
            var listResponse = await _client.GetAsync("/api/users/many_arrays_user/datemarks");
            listResponse.EnsureSuccessStatusCode();
            
            var dateMarks = await listResponse.Content.ReadFromJsonAsync<List<DateMark>>();
            dateMarks.Should().HaveCount(1);
            
            var dateMark = dateMarks!.First();
            dateMark.Categories.Should().HaveCount(50);
            dateMark.Tags.Should().HaveCount(50);
            dateMark.Qualities.Should().HaveCount(50);
        }
    }

    #endregion

    #region Boundary Value Tests

    [Fact]
    public async Task DateMark_Create_WithBoundaryCoordinates_HandlesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Test with boundary coordinate values
        var boundaryTests = new[]
        {
            (90.0, 180.0, "max_lat_lng"),
            (-90.0, -180.0, "min_lat_lng"),
            (0.0, 0.0, "zero_lat_lng"),
            (90.0, -180.0, "max_lat_min_lng"),
            (-90.0, 180.0, "min_lat_max_lng"),
            (89.999999, 179.999999, "near_max"),
            (-89.999999, -179.999999, "near_min")
        };

        foreach (var (lat, lng, testId) in boundaryTests)
        {
            var request = new UpsertDateMarkRequest(
                Id: $"boundary_{testId}",
                UserId: "test_user_id", // Match TestAuthenticationService
                Latitude: lat,
                Longitude: lng,
                PlaceId: null,
                PlaceName: $"Boundary Test {testId}",
                PlaceTypes: null,
                PlaceRating: null,
                PlacePriceLevel: null,
                Address: null,
                City: null,
                Country: null,
                Categories: new[] { "Test" },
                Tags: Array.Empty<string>(),
                Qualities: Array.Empty<string>(),
                Notes: $"Testing boundary coordinates: {lat}, {lng}",
                VisitDate: new DateOnly(2025, 8, 12),
                Visibility: "public"
            );

            var response = await _client.PostAsJsonAsync("/api/datemarks", request);
            response.EnsureSuccessStatusCode();
        }

        // Verify all boundary tests were created
        var listResponse = await _client.GetAsync("/api/users/test_user_id/datemarks");
        listResponse.EnsureSuccessStatusCode();
        
        var dateMarks = await listResponse.Content.ReadFromJsonAsync<List<DateMark>>();
        dateMarks.Should().HaveCount(7);
    }

    #endregion
}
