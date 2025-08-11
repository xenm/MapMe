using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MapMe.DTOs;
using MapMe.Models;
using Xunit;

namespace MapMe.Tests;

public class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Profiles_Create_And_Get()
    {
        var client = _factory.CreateClient();
        var req = new CreateProfileRequest(
            Id: "p_test",
            UserId: "u_test",
            DisplayName: "Tester",
            Bio: null,
            Photos: System.Array.Empty<UserPhoto>(),
            PreferredCategories: System.Array.Empty<string>(),
            Visibility: "public");
        var post = await client.PostAsJsonAsync("/api/profiles", req);
        post.EnsureSuccessStatusCode();
        var get = await client.GetAsync("/api/profiles/p_test");
        get.EnsureSuccessStatusCode();
        var profile = await get.Content.ReadFromJsonAsync<UserProfile>();
        profile.Should().NotBeNull();
        profile!.Id.Should().Be("p_test");
    }

    [Fact]
    public async Task DateMarks_Create_And_List_By_User()
    {
        var client = _factory.CreateClient();
        var dmReq = new UpsertDateMarkRequest(
            Id: "dm_test",
            UserId: "u_test",
            Latitude: 37.0,
            Longitude: -122.0,
            PlaceId: null,
            PlaceName: "Test Place",
            PlaceTypes: new[] { "cafe" },
            PlaceRating: null,
            PlacePriceLevel: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "coffee" },
            Tags: new[] { "cozy" },
            Qualities: new[] { "romantic" },
            Notes: "nice",
            VisitDate: new DateOnly(2025, 8, 9),
            Visibility: "public");
        var post = await client.PostAsJsonAsync("/api/datemarks", dmReq);
        post.EnsureSuccessStatusCode();
        var list = await client.GetFromJsonAsync<System.Collections.Generic.List<DateMark>>("/api/users/u_test/datemarks");
        list.Should().NotBeNull();
        list!.Should().ContainSingle(x => x.Id == "dm_test");
    }
}
