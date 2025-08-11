using System;
using System.Collections.Generic;
using MapMe.Models;
using MapMe.Utils;

namespace MapMe.DTOs;

public sealed record CreateProfileRequest(
    string Id,
    string UserId,
    string DisplayName,
    string? Bio,
    IReadOnlyList<UserPhoto>? Photos,
    IReadOnlyList<string>? PreferredCategories,
    string Visibility
)
{
    public UserProfile ToProfile(DateTimeOffset now) => new(
        Id: Id,
        UserId: UserId,
        DisplayName: DisplayName,
        Bio: Bio,
        Photos: Photos ?? Array.Empty<UserPhoto>(),
        Preferences: new UserPreferences(PreferredCategories ?? Array.Empty<string>()),
        Visibility: Visibility,
        CreatedAt: now,
        UpdatedAt: now
    );
}

public sealed record UpsertDateMarkRequest(
    string Id,
    string UserId,
    double Latitude,
    double Longitude,
    string? PlaceId,
    string? PlaceName,
    IReadOnlyList<string>? PlaceTypes,
    double? PlaceRating,
    int? PlacePriceLevel,
    string? Address,
    string? City,
    string? Country,
    IReadOnlyList<string>? Categories,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? Qualities,
    string? Notes,
    DateOnly? VisitDate,
    string Visibility
)
{
    public DateMark ToDateMark(DateTimeOffset now)
    {
        var geo = GeoPoint.FromLatLng(Latitude, Longitude);
        // Prototype geo hash/prefix via tile key; replace with real geohash later
        var tileKey = MapMe.Utils.Geo.TileKey(Latitude, Longitude, precision: 3);
        var types = PlaceTypes ?? Array.Empty<string>();
        var place = PlaceName is null && PlaceId is null && types.Count == 0 && PlaceRating is null && PlacePriceLevel is null
            ? null
            : new PlaceSnapshot(PlaceName ?? string.Empty, types, PlaceRating, PlacePriceLevel);
        return new DateMark(
            Id: Id,
            UserId: UserId,
            Geo: geo,
            GeoHash: tileKey,
            GeoHashPrefix: tileKey,
            PlaceId: PlaceId,
            PlaceSnapshot: place,
            Address: Address,
            City: City,
            Country: Country,
            Categories: Categories ?? Array.Empty<string>(),
            CategoriesNorm: Normalization.ToNorm(Categories ?? Array.Empty<string>()),
            Tags: Tags ?? Array.Empty<string>(),
            TagsNorm: Normalization.ToNorm(Tags ?? Array.Empty<string>()),
            Qualities: Qualities ?? Array.Empty<string>(),
            QualitiesNorm: Normalization.ToNorm(Qualities ?? Array.Empty<string>()),
            Notes: Notes,
            VisitDate: VisitDate,
            Visibility: Visibility,
            CreatedAt: now,
            UpdatedAt: now,
            IsDeleted: false
        );
    }
}
