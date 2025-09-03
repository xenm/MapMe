namespace MapMe.Models;

public sealed record UserPhoto(
    string Url,
    bool IsPrimary
);

public sealed record UserPreferences(
    IReadOnlyList<string> Categories
);

public sealed record UserProfile(
    string Id,
    string UserId,
    string DisplayName,
    string? Bio,
    IReadOnlyList<UserPhoto> Photos,
    UserPreferences? Preferences,
    string Visibility,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);