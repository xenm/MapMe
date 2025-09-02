using System.Text.Json.Serialization;

namespace MapMe.Models;

public sealed record GeoPoint(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("coordinates")]
    IReadOnlyList<double> Coordinates
)
{
    public static GeoPoint FromLatLng(double lat, double lng) => new("Point", new[] { lng, lat });
}

public sealed record PlaceSnapshot(
    string Name,
    IReadOnlyList<string> Types,
    double? Rating,
    int? PriceLevel
);

public sealed record DateMark(
    string Id,
    string UserId,
    GeoPoint Geo,
    string GeoHash,
    string GeoHashPrefix,
    string? PlaceId,
    PlaceSnapshot? PlaceSnapshot,
    string? Address,
    string? City,
    string? Country,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> CategoriesNorm,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> TagsNorm,
    IReadOnlyList<string> Qualities,
    IReadOnlyList<string> QualitiesNorm,
    string? Notes,
    DateOnly? VisitDate,
    string Visibility,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDeleted
);