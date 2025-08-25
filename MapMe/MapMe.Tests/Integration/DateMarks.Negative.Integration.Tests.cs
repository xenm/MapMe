using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MapMe.Repositories;
using MapMe.Services;
using MapMe.DTOs;
using Xunit;

namespace MapMe.Tests;

[Trait("Category", "Integration")]
public class DateMarksNegativeIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DateMarksNegativeIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var web = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Ensure in-memory repos + test auth
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

        _client = web.CreateClient();
    }

    [Fact]
    public async Task DateMark_Create_WithoutAuth_ReturnsUnauthorized()
    {
        var req = new UpsertDateMarkRequest(
            Id: "dm_neg1",
            UserId: "test_user_id",
            Latitude: 10,
            Longitude: 10,
            PlaceId: null,
            PlaceName: null,
            PlaceTypes: null,
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
            Visibility: "public");

        var resp = await _client.PostAsJsonAsync("/api/datemarks", req);
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DateMark_Create_MissingRequiredId_ReturnsBadRequest()
    {
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        var req = new UpsertDateMarkRequest(
            Id: "", // invalid: required by API
            UserId: "test_user_id",
            Latitude: 0,
            Longitude: 0,
            PlaceId: null,
            PlaceName: null,
            PlaceTypes: null,
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
            Visibility: "public");

        var resp = await _client.PostAsJsonAsync("/api/datemarks", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
