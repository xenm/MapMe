using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace MapMe.Tests.Integration;

[Trait("Category", "Integration")]
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
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

    /// <summary>
    /// Creates a test user and returns a valid JWT token for authentication
    /// </summary>
    private async Task<string> CreateTestUserAndGetTokenAsync(string username = "test_user", string email = "test@example.com", string password = "TestPassword123!")
    {
        // Register a test user
        var registerRequest = new RegisterRequest(
            Username: username,
            Email: email,
            Password: password,
            ConfirmPassword: password,
            DisplayName: "Test User"
        );

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        
        // If user already exists, try to login instead
        if (!registerResponse.IsSuccessStatusCode)
        {
            var loginRequest = new LoginRequest(
                Username: username,
                Password: password,
                RememberMe: false
            );

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            if (loginResponse.IsSuccessStatusCode)
            {
                var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
                return loginResult!.Token!;
            }
        }
        else
        {
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
            return registerResult!.Token!;
        }

        throw new InvalidOperationException("Failed to create test user or login");
    }

    /// <summary>
    /// Adds JWT authentication header to HTTP client for subsequent requests
    /// </summary>
    private void AddAuthenticationHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task UserProfile_CompleteWorkflow_CreatesAndRetrievesProfile()
    {
        // Arrange - Create authenticated user
        var username = "user_integration_test";
        var token = await CreateTestUserAndGetTokenAsync(username, "integration@example.com");
        AddAuthenticationHeader(token);
        
        // Get the actual user ID from the authentication response
        var validateResponse = await _client.GetAsync("/api/auth/validate-token");
        var authenticatedUser = await validateResponse.Content.ReadFromJsonAsync<AuthenticatedUser>();
        var actualUserId = authenticatedUser!.UserId;
        
        var createRequest = new CreateProfileRequest(
            Id: "profile_integration_test",
            UserId: actualUserId, // Use the actual user ID from authentication
            DisplayName: "Integration Test User",
            Bio: "This is a test user for integration testing",
            Photos: new[] 
            { 
                new UserPhoto("https://example.com/photo1.jpg", true),
                new UserPhoto("https://example.com/photo2.jpg", false)
            },
            PreferredCategories: new[] { "restaurants", "cafes", "bars" },
            Visibility: "public"
        );

        // Act - Create profile
        var createResponse = await _client.PostAsJsonAsync("/api/profiles", createRequest);
        
        // Assert - Profile created successfully
        createResponse.EnsureSuccessStatusCode();

        // Act - Retrieve profile
        var getResponse = await _client.GetAsync("/api/profiles/profile_integration_test");
        
        // Assert - Profile retrieved successfully
        getResponse.EnsureSuccessStatusCode();
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfile>();
        
        profile.Should().NotBeNull();
        profile!.Id.Should().Be("profile_integration_test");
        profile.DisplayName.Should().Be("Integration Test User");
        profile.Bio.Should().Be("This is a test user for integration testing");
        profile.Photos.Should().HaveCount(2);
        profile.Photos.First().Url.Should().Be("https://example.com/photo1.jpg");
        profile.Preferences!.Categories.Should().Contain("restaurants");
    }

    [Fact]
    public async Task DateMark_CompleteWorkflow_CreatesAndListsDateMarks()
    {
        // Arrange - Create authenticated user
        var userId = "test_user_id"; // Match TestAuthenticationService
        var token = await CreateTestUserAndGetTokenAsync(userId, "datemark@example.com");
        AddAuthenticationHeader(token);
        
        var dateMarkRequest = new UpsertDateMarkRequest(
            Id: "datemark_integration_test",
            UserId: userId,
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: "ChIJVVVVVVVVVVVVVVVVVVVVVV",
            PlaceName: "Integration Test Restaurant",
            PlaceTypes: new[] { "restaurant", "food", "establishment" },
            PlaceRating: 4.5,
            PlacePriceLevel: 2,
            Address: "123 Test Street, San Francisco, CA 94102",
            City: "San Francisco",
            Country: "United States",
            Categories: new[] { "Fine Dining", "Italian" },
            Tags: new[] { "Romantic", "Special Occasion" },
            Qualities: new[] { "Excellent Service", "Great Atmosphere" },
            Notes: "Perfect place for anniversary dinner!",
            VisitDate: new DateOnly(2025, 8, 10),
            Visibility: "public"
        );

        // Act - Create DateMark
        var createResponse = await _client.PostAsJsonAsync("/api/datemarks", dateMarkRequest);
        
        // Assert - DateMark created successfully
        createResponse.EnsureSuccessStatusCode();

        // Act - List DateMarks for user
        var listResponse = await _client.GetAsync($"/api/users/{userId}/datemarks");
        
        // Assert - DateMarks retrieved successfully
        listResponse.EnsureSuccessStatusCode();
        var dateMarks = await listResponse.Content.ReadFromJsonAsync<List<DateMark>>();
        
        dateMarks.Should().NotBeNull();
        dateMarks!.Should().HaveCount(1);
        
        var dateMark = dateMarks!.First();
        dateMark.Id.Should().Be("datemark_integration_test");
        dateMark.UserId.Should().Be(userId);
        dateMark.Geo.Coordinates[1].Should().Be(37.7749); // Latitude at index 1
        dateMark.Geo.Coordinates[0].Should().Be(-122.4194); // Longitude at index 0
        dateMark.PlaceSnapshot!.Name.Should().Be("Integration Test Restaurant");
        dateMark.Categories.Should().Contain("Fine Dining");
        dateMark.Tags.Should().Contain("Romantic");
        dateMark.Qualities.Should().Contain("Excellent Service");
        dateMark.Notes.Should().Be("Perfect place for anniversary dinner!");
        dateMark.VisitDate.Should().Be(new DateOnly(2025, 8, 10));
    }

    [Fact]
    public async Task DateMark_FilteringByCategories_ReturnsCorrectResults()
    {
        // Arrange - Create authenticated user
        var userId = "test_user_id"; // Match TestAuthenticationService
        var token = await CreateTestUserAndGetTokenAsync(userId, "filter@example.com");
        AddAuthenticationHeader(token);
        
        // Create multiple DateMarks with different categories
        var restaurantMark = new UpsertDateMarkRequest(
            Id: "restaurant_mark",
            UserId: userId,
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: null,
            PlaceName: "Test Restaurant",
            PlaceTypes: new[] { "restaurant" },
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "Restaurant", "Italian" },
            Tags: new[] { "Romantic" },
            Qualities: new[] { "Great Food" },
            Notes: "Great dinner",
            VisitDate: new DateOnly(2025, 8, 10),
            Visibility: "public"
        );

        var cafeMark = new UpsertDateMarkRequest(
            Id: "cafe_mark",
            UserId: userId,
            Latitude: 37.7849,
            Longitude: -122.4094,
            PlaceId: null,
            PlaceName: "Test Cafe",
            PlaceTypes: new[] { "cafe" },
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "Cafe", "Coffee" },
            Tags: new[] { "Cozy" },
            Qualities: new[] { "Good Coffee" },
            Notes: "Morning coffee",
            VisitDate: new DateOnly(2025, 8, 11),
            Visibility: "public"
        );

        // Create both DateMarks
        await _client.PostAsJsonAsync("/api/datemarks", restaurantMark);
        await _client.PostAsJsonAsync("/api/datemarks", cafeMark);

        // Act - Filter by restaurant category
        var response = await _client.GetAsync($"/api/users/{userId}/datemarks?categories=restaurant");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var dateMarks = await response.Content.ReadFromJsonAsync<List<DateMark>>();
        
        dateMarks.Should().NotBeNull();
        dateMarks!.Should().HaveCount(1);
        dateMarks!.First().Id.Should().Be("restaurant_mark");
    }

    [Fact]
    public async Task DateMark_FilteringByDateRange_ReturnsCorrectResults()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        var userId = "test_user_id"; // Match TestAuthenticationService
        
        var oldMark = new UpsertDateMarkRequest(
            Id: "old_mark",
            UserId: userId,
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: null,
            PlaceName: "Old Place",
            PlaceTypes: new[] { "restaurant" },
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "Restaurant" },
            Tags: Array.Empty<string>(),
            Qualities: Array.Empty<string>(),
            Notes: "Old visit",
            VisitDate: new DateOnly(2025, 7, 1),
            Visibility: "public"
        );

        var newMark = new UpsertDateMarkRequest(
            Id: "new_mark",
            UserId: userId,
            Latitude: 37.7849,
            Longitude: -122.4094,
            PlaceId: null,
            PlaceName: "New Place",
            PlaceTypes: new[] { "restaurant" },
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "Restaurant" },
            Tags: Array.Empty<string>(),
            Qualities: Array.Empty<string>(),
            Notes: "Recent visit",
            VisitDate: new DateOnly(2025, 8, 15),
            Visibility: "public"
        );

        // Create both DateMarks
        await _client.PostAsJsonAsync("/api/datemarks", oldMark);
        await _client.PostAsJsonAsync("/api/datemarks", newMark);

        // Act - Filter by date range (August 2025)
        var response = await _client.GetAsync($"/api/users/{userId}/datemarks?from=2025-08-01&to=2025-08-31");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var dateMarks = await response.Content.ReadFromJsonAsync<List<DateMark>>();
        
        dateMarks.Should().NotBeNull();
        dateMarks!.Should().HaveCount(1);
        dateMarks!.First().Id.Should().Be("new_mark");
    }

    [Fact]
    public async Task Profile_NotFound_Returns404()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-session-token");
        
        // Act
        var response = await _client.GetAsync("/api/profiles/non_existent_profile");
        
        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DateMarks_EmptyUser_ReturnsEmptyList()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer test-session-token");
        
        // Act
        var response = await _client.GetAsync("/api/users/non_existent_user/datemarks");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var dateMarks = await response.Content.ReadFromJsonAsync<List<DateMark>>();
        
        dateMarks.Should().NotBeNull();
        dateMarks!.Should().BeEmpty();
    }

    [Fact]
    public async Task DateMark_UpdateExisting_ModifiesCorrectly()
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        var userId = "test_user_id"; // Match TestAuthenticationService
        var originalRequest = new UpsertDateMarkRequest(
            Id: "update_test_mark",
            UserId: userId,
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: null,
            PlaceName: "Original Name",
            PlaceTypes: new[] { "restaurant" },
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "Restaurant" },
            Tags: new[] { "Original Tag" },
            Qualities: new[] { "Original Quality" },
            Notes: "Original notes",
            VisitDate: new DateOnly(2025, 8, 10),
            Visibility: "public"
        );

        // Create original DateMark
        await _client.PostAsJsonAsync("/api/datemarks", originalRequest);

        // Arrange - Updated request
        var updatedRequest = originalRequest with
        {
            PlaceName = "Updated Name",
            Categories = new[] { "Updated Category" },
            Tags = new[] { "Updated Tag" },
            Qualities = new[] { "Updated Quality" },
            Notes = "Updated notes"
        };

        // Act - Update DateMark
        var updateResponse = await _client.PostAsJsonAsync("/api/datemarks", updatedRequest);
        updateResponse.EnsureSuccessStatusCode();

        // Act - Retrieve updated DateMark
        var listResponse = await _client.GetAsync($"/api/users/{userId}/datemarks");
        var dateMarks = await listResponse.Content.ReadFromJsonAsync<List<DateMark>>();

        // Assert
        dateMarks.Should().HaveCount(1);
        var dateMark = dateMarks!.First();
        dateMark.PlaceSnapshot!.Name.Should().Be("Updated Name");
        dateMark.Categories.Should().Contain("Updated Category");
        dateMark.Tags.Should().Contain("Updated Tag");
        dateMark.Qualities.Should().Contain("Updated Quality");
        dateMark.Notes.Should().Be("Updated notes");
    }

    [Theory]
    [InlineData("public")]
    [InlineData("friends")]
    [InlineData("private")]
    public async Task DateMark_VisibilitySettings_AreRespected(string visibility)
    {
        // Arrange - Add authentication
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        
        var userId = "test_user_id"; // Match TestAuthenticationService
        var request = new UpsertDateMarkRequest(
            Id: $"visibility_test_{visibility}",
            UserId: userId,
            Latitude: 37.7749,
            Longitude: -122.4194,
            PlaceId: null,
            PlaceName: "Visibility Test Place",
            PlaceTypes: new[] { "restaurant" },
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "Restaurant" },
            Tags: Array.Empty<string>(),
            Qualities: Array.Empty<string>(),
            Notes: $"Testing {visibility} visibility",
            VisitDate: new DateOnly(2025, 8, 10),
            Visibility: visibility
        );

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/datemarks", request);
        createResponse.EnsureSuccessStatusCode();

        var listResponse = await _client.GetAsync($"/api/users/{userId}/datemarks");
        var dateMarks = await listResponse.Content.ReadFromJsonAsync<List<DateMark>>();

        // Assert
        dateMarks.Should().HaveCount(1);
        dateMarks!.First().Visibility.Should().Be(visibility);
    }
}
