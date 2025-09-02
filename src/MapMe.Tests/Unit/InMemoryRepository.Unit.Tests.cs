using FluentAssertions;
using MapMe.Models;
using MapMe.Repositories;
using MapMe.Utils;
using Xunit;

namespace MapMe.Tests.Unit;

public class InMemoryRepositoryTests
{
    [Fact]
    public async Task UserProfile_InMemory_Upsert_And_Get()
    {
        var repo = new InMemoryUserProfileRepository();
        var now = DateTimeOffset.UtcNow;
        var p = new UserProfile(
            Id: "p1",
            UserId: "u1",
            DisplayName: "Alex",
            Bio: null,
            Photos: Array.Empty<UserPhoto>(),
            Preferences: new UserPreferences(Array.Empty<string>()),
            Visibility: "public",
            CreatedAt: now,
            UpdatedAt: now);
        await repo.UpsertAsync(p);
        (await repo.GetByIdAsync("p1")).Should().NotBeNull();
        (await repo.GetByUserIdAsync("u1")).Should().NotBeNull();
    }

    [Fact]
    public async Task DateMarks_InMemory_Filtering_Works()
    {
        var repo = new InMemoryDateMarkByUserRepository();
        var now = DateTimeOffset.UtcNow;
        var dm1 = new DateMark(
            Id: "dm1",
            UserId: "u1",
            Geo: GeoPoint.FromLatLng(37.0, -122.0),
            GeoHash: "t",
            GeoHashPrefix: "t",
            PlaceId: null,
            PlaceSnapshot: null,
            Address: null,
            City: null,
            Country: null,
            Categories: new[] { "coffee" },
            CategoriesNorm: Normalization.ToNorm("coffee"),
            Tags: new[] { "cozy" },
            TagsNorm: Normalization.ToNorm("cozy"),
            Qualities: new[] { "romantic" },
            QualitiesNorm: Normalization.ToNorm("romantic"),
            Notes: null,
            VisitDate: new DateOnly(2025, 8, 9),
            Visibility: "public",
            CreatedAt: now,
            UpdatedAt: now,
            IsDeleted: false);
        var dm2 = dm1 with
        {
            Id = "dm2", Categories = new[] { "art" }, CategoriesNorm = Normalization.ToNorm("art"),
            VisitDate = new DateOnly(2025, 8, 10)
        };
        await repo.UpsertAsync(dm1);
        await repo.UpsertAsync(dm2);

        var all = await repo.GetByUserAsync("u1").ToListAsync();
        all.Should().HaveCount(2);

        var onlyCoffee = await repo.GetByUserAsync("u1", categories: Normalization.ToNorm("coffee")).ToListAsync();
        onlyCoffee.Should().HaveCount(1);
        onlyCoffee.First().Id.Should().Be("dm1");

        var window = await repo.GetByUserAsync("u1", from: new DateOnly(2025, 8, 9), to: new DateOnly(2025, 8, 9))
            .ToListAsync();
        window.Should().HaveCount(1);
        window.First().Id.Should().Be("dm1");
    }
}

internal static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source) list.Add(item);
        return list;
    }
}