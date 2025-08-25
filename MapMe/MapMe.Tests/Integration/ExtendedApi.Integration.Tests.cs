using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using MapMe.Services;
using Xunit;

namespace MapMe.Tests;

/// <summary>
/// Extended integration tests covering advanced scenarios, edge cases, and error conditions
/// for the MapMe API endpoints.
/// </summary>
[Trait("Category", "Integration")]
public class ExtendedApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExtendedApiIntegrationTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task Profile_Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Test missing required fields
        var invalidRequests = new[]
        {
            new CreateProfileRequest("", "user1", "Name", null, null, null, "public"), // Empty Id
            new CreateProfileRequest("profile1", "", "Name", null, null, null, "public"), // Empty UserId
            new CreateProfileRequest("profile1", "user1", "", null, null, null, "public"), // Empty DisplayName
        };

        foreach (var request in invalidRequests)
        {
            var response = await _client.PostAsJsonAsync("/api/profiles", request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Profile_Update_ExistingProfile_ModifiesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Arrange - Create initial profile
        var initialRequest = new CreateProfileRequest(
            Id: "update_profile_test",
            UserId: "test_user_id", // Match TestAuthenticationService
            DisplayName: "Initial Name",
            Bio: "Initial bio",
            Photos: new[] { new UserPhoto("https://example.com/initial.jpg", true) },
            PreferredCategories: new[] { "restaurants" },
            Visibility: "public"
        );

        await _client.PostAsJsonAsync("/api/profiles", initialRequest);
        
        // Small delay to ensure different timestamps
        await Task.Delay(10);

        // Act - Update profile with new data
        var updatedRequest = new CreateProfileRequest(
            Id: "update_profile_test", // Same ID for update
            UserId: "test_user_id", // Match TestAuthenticationService
            DisplayName: "Updated Name",
            Bio: "Updated bio with more content",
            Photos: new[]
            {
                new UserPhoto("https://example.com/updated1.jpg", true),
                new UserPhoto("https://example.com/updated2.jpg", false)
            },
            PreferredCategories: new[] { "restaurants", "cafes", "bars" },
            Visibility: "friends"
        );

        var updateResponse = await _client.PostAsJsonAsync("/api/profiles", updatedRequest);
        updateResponse.EnsureSuccessStatusCode();

        // Assert - Verify updates
        var getResponse = await _client.GetAsync("/api/profiles/update_profile_test");
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfile>();
        
        profile.Should().NotBeNull();
        profile!.DisplayName.Should().Be("Updated Name");
        profile.Bio.Should().Be("Updated bio with more content");
        profile.Photos.Should().HaveCount(2);
        profile.Preferences!.Categories.Should().HaveCount(3);
        profile.Visibility.Should().Be("friends");
        profile.UpdatedAt.Should().BeOnOrAfter(profile.CreatedAt);
    }

    [Fact]
    public async Task DateMark_Create_WithInvalidData_ReturnsBadRequest()
    {
        // Test missing required fields
        var invalidRequests = new[]
        {
            new UpsertDateMarkRequest("", "user1", 37.0, -122.0, null, "Place", null, null, null, null, null, null, null, null, null, null, null, "public"), // Empty Id
            new UpsertDateMarkRequest("dm1", "", 37.0, -122.0, null, "Place", null, null, null, null, null, null, null, null, null, null, null, "public"), // Empty UserId
        };

        foreach (var request in invalidRequests)
        {
            var response = await _client.PostAsJsonAsync("/api/datemarks", request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task DateMark_Create_WithExtremeCoordinates_HandlesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Test boundary coordinates
        var extremeCoordinates = new[]
        {
            (90.0, 180.0),   // North Pole, International Date Line
            (-90.0, -180.0), // South Pole, International Date Line
            (0.0, 0.0),      // Null Island
            (85.0511, 180.0), // Near maximum latitude for Web Mercator
            (-85.0511, -180.0) // Near minimum latitude for Web Mercator
        };

        for (int i = 0; i < extremeCoordinates.Length; i++)
        {
            var (lat, lng) = extremeCoordinates[i];
            var request = new UpsertDateMarkRequest(
                Id: $"extreme_coords_{i}",
                UserId: "test_user_id", // Match TestAuthenticationService
                Latitude: lat,
                Longitude: lng,
                PlaceId: null,
                PlaceName: $"Extreme Location {i}",
                PlaceTypes: new[] { "test" },
                PlaceRating: null,
                PlacePriceLevel: null,
                Address: null,
                City: null,
                Country: null,
                Categories: new[] { "Test" },
                Tags: Array.Empty<string>(),
                Qualities: Array.Empty<string>(),
                Notes: $"Testing extreme coordinates: {lat}, {lng}",
                VisitDate: new DateOnly(2025, 8, 12),
                Visibility: "public"
            );

            var response = await _client.PostAsJsonAsync("/api/datemarks", request);
            response.EnsureSuccessStatusCode();
        }

        // Verify all extreme coordinate DateMarks were created
        var listResponse = await _client.GetAsync("/api/users/test_user_id/datemarks");
        var dateMarks = await listResponse.Content.ReadFromJsonAsync<List<DateMark>>();
        
        dateMarks.Should().HaveCount(5);
    }

    [Fact]
    public async Task DateMark_FilteringCombinations_ReturnsCorrectResults()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Arrange - Create DateMarks with various combinations
        var userId = "test_user_id"; // Match TestAuthenticationService
        var dateMarks = new[]
        {
            new UpsertDateMarkRequest("combo1", userId, 37.0, -122.0, null, "Restaurant A", null, null, null, null, null, null,
                new[] { "Restaurant", "Italian" }, new[] { "Romantic", "Expensive" }, new[] { "Great Food", "Good Service" },
                "Perfect for dates", new DateOnly(2025, 8, 1), "public"),
            
            new UpsertDateMarkRequest("combo2", userId, 37.1, -122.1, null, "Cafe B", null, null, null, null, null, null,
                new[] { "Cafe", "Coffee" }, new[] { "Casual", "Quiet" }, new[] { "Good Coffee", "Cozy" },
                "Great for morning meetings", new DateOnly(2025, 8, 5), "public"),
            
            new UpsertDateMarkRequest("combo3", userId, 37.2, -122.2, null, "Bar C", null, null, null, null, null, null,
                new[] { "Bar", "French" }, new[] { "Upscale", "Loud" }, new[] { "Great Drinks", "Good Service" },
                "Fun night out", new DateOnly(2025, 8, 10), "public")
        };

        foreach (var dm in dateMarks)
        {
            await _client.PostAsJsonAsync("/api/datemarks", dm);
        }

        // Test filter combinations
        var response = await _client.GetAsync($"/api/users/{userId}/datemarks?categories=italian&tags=romantic");
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<DateMark>>();
        results.Should().HaveCount(1, "Should find only Restaurant A with Italian AND Romantic");
        results!.First().PlaceSnapshot!.Name.Should().Be("Restaurant A");
        
        response = await _client.GetAsync($"/api/users/{userId}/datemarks?categories=restaurant&categories=cafe");
        response.EnsureSuccessStatusCode();
        results = await response.Content.ReadFromJsonAsync<List<DateMark>>();
        results.Should().HaveCount(2, "Should find Restaurant OR Cafe");
    }

    [Fact]
    public async Task MapDateMarks_Query_ReturnsEmptyForPrototype()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // The current implementation returns empty results as noted in the code
        var response = await _client.GetAsync("/api/map/datemarks?lat=37.7749&lng=-122.4194&radiusMeters=1000");
        
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<DateMark>>();
        
        results.Should().NotBeNull();
        results.Should().BeEmpty(); // Expected for prototype implementation
    }

    [Fact]
    public async Task ConcurrentOperations_MultipleProfileCreation_HandlesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Test concurrent profile creation to ensure thread safety
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 5; i++)
        {
            var request = new CreateProfileRequest(
                Id: $"concurrent_profile_{i}",
                UserId: "test_user_id", // Match TestAuthenticationService
                DisplayName: $"Concurrent User {i}",
                Bio: $"Bio for user {i}",
                Photos: Array.Empty<UserPhoto>(),
                PreferredCategories: new[] { "restaurants" },
                Visibility: "public"
            );
            
            tasks.Add(_client.PostAsJsonAsync("/api/profiles", request));
        }

        var responses = await Task.WhenAll(tasks);
        
        // All requests should succeed
        foreach (var response in responses)
        {
            response.EnsureSuccessStatusCode();
        }

        // Verify all profiles were created
        for (int i = 0; i < 5; i++)
        {
            var getResponse = await _client.GetAsync($"/api/profiles/concurrent_profile_{i}");
            getResponse.EnsureSuccessStatusCode();
            
            var profile = await getResponse.Content.ReadFromJsonAsync<UserProfile>();
            profile.Should().NotBeNull();
            profile!.DisplayName.Should().Be($"Concurrent User {i}");
        }
    }

    [Fact]
    public async Task DateMark_EmptyArraysAndNulls_HandlesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Test with explicit empty arrays vs null values
        var request = new UpsertDateMarkRequest(
            Id: "empty_arrays_test",
            UserId: "test_user_id", // Match TestAuthenticationService
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: null,
            PlaceName: "Empty Arrays Test Place",
            PlaceTypes: Array.Empty<string>(),
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: Array.Empty<string>(),
            Tags: Array.Empty<string>(),
            Qualities: Array.Empty<string>(),
            Notes: null,
            VisitDate: null,
            Visibility: "public"
        );

        var response = await _client.PostAsJsonAsync("/api/datemarks", request);
        response.EnsureSuccessStatusCode();

        // Verify the DateMark was created with empty collections
        var listResponse = await _client.GetAsync("/api/users/test_user_id/datemarks");
        var dateMarks = await listResponse.Content.ReadFromJsonAsync<List<DateMark>>();
        
        dateMarks.Should().HaveCount(1);
        var dateMark = dateMarks!.First();
        dateMark.Categories.Should().BeEmpty();
        dateMark.Tags.Should().BeEmpty();
        dateMark.Qualities.Should().BeEmpty();
        dateMark.Notes.Should().BeNull();
        dateMark.VisitDate.Should().BeNull();
    }

    [Fact]
    public async Task DateMark_DateRangeEdgeCases_HandlesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        // Arrange - Create DateMarks across different time periods
        var userId = "test_user_id"; // Match TestAuthenticationService
        var dateMarks = new[]
        {
            new UpsertDateMarkRequest("date1", userId, 37.0, -122.0, null, "Place 1", null, null, null, null, null, null,
                new[] { "Restaurant" }, null, null, "Visit 1", new DateOnly(2025, 1, 1), "public"), // New Year
            
            new UpsertDateMarkRequest("date2", userId, 37.0, -122.0, null, "Place 2", null, null, null, null, null, null,
                new[] { "Restaurant" }, null, null, "Visit 2", new DateOnly(2025, 2, 28), "public"), // Last day of February
            
            new UpsertDateMarkRequest("date3", userId, 37.0, -122.0, null, "Place 3", null, null, null, null, null, null,
                new[] { "Restaurant" }, null, null, "Visit 3", new DateOnly(2025, 12, 31), "public"), // New Year's Eve
        };

        foreach (var dm in dateMarks)
        {
            await _client.PostAsJsonAsync("/api/datemarks", dm);
        }

        // Test edge cases for date filtering
        var response = await _client.GetAsync($"/api/users/{userId}/datemarks?from=2025-01-01&to=2025-01-01");
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<List<DateMark>>();
        results.Should().HaveCount(1, "Should find exact single day match");

        response = await _client.GetAsync($"/api/users/{userId}/datemarks?from=2025-01-01&to=2025-12-31");
        response.EnsureSuccessStatusCode();
        results = await response.Content.ReadFromJsonAsync<List<DateMark>>();
        results.Should().HaveCount(3, "Should find entire year matches");
    }
}
