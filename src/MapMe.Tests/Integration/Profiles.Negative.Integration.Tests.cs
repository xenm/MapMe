using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using MapMe.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MapMe.Tests.Integration;

[Trait("Category", "Integration")]
public class ProfilesNegativeIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public ProfilesNegativeIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Ensure we use in-memory repos and test auth
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(IUserProfileRepository) ||
                    d.ServiceType == typeof(IDateMarkByUserRepository) ||
                    d.ServiceType == typeof(IAuthenticationService)).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
                services.AddSingleton<IDateMarkByUserRepository, InMemoryDateMarkByUserRepository>();
                services.AddScoped<IAuthenticationService, TestAuthenticationService>();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Profile_Create_WithoutAuth_ReturnsUnauthorized()
    {
        // no auth header
        var request = new CreateProfileRequest(
            Id: "p_neg1",
            UserId: "u_neg1",
            DisplayName: "NoAuth",
            Bio: null,
            Photos: Array.Empty<UserPhoto>(),
            PreferredCategories: Array.Empty<string>(),
            Visibility: "public");

        var resp = await _client.PostAsJsonAsync("/api/profiles", request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Profile_Create_InvalidVisibility_ReturnsBadRequest()
    {
        // with auth header
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-session-token");

        var request = new CreateProfileRequest(
            Id: "p_neg2",
            UserId: "u_neg2",
            DisplayName: "BadVisibility",
            Bio: null,
            Photos: Array.Empty<UserPhoto>(),
            PreferredCategories: Array.Empty<string>(),
            Visibility: "invalid_visibility_value");

        var resp = await _client.PostAsJsonAsync("/api/profiles", request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        // Expect input validation to fail
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }
}