using System.Text.Json.Serialization;

namespace MapMe.Client.Models;

/// <summary>
/// Enhanced user profile with Tinder-style dating app fields
/// </summary>
public class UserProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("lookingFor")]
    public string? LookingFor { get; set; }

    [JsonPropertyName("relationshipType")]
    public string? RelationshipType { get; set; }

    [JsonPropertyName("height")]
    public string? Height { get; set; }

    [JsonPropertyName("education")]
    public string? Education { get; set; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("hometown")]
    public string? Hometown { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = new();

    [JsonPropertyName("interests")]
    public List<string> Interests { get; set; } = new();

    [JsonPropertyName("hobbies")]
    public List<string> Hobbies { get; set; } = new();

    [JsonPropertyName("favoriteCategories")]
    public List<string> FavoriteCategories { get; set; } = new();

    [JsonPropertyName("lifestyle")]
    public LifestylePreferences? Lifestyle { get; set; }

    [JsonPropertyName("photos")]
    public List<UserPhoto> Photos { get; set; } = new();

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "public";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class LifestylePreferences
{
    [JsonPropertyName("smoking")]
    public string? Smoking { get; set; } // "never", "socially", "regularly"

    [JsonPropertyName("drinking")]
    public string? Drinking { get; set; } // "never", "socially", "regularly"

    [JsonPropertyName("exercise")]
    public string? Exercise { get; set; } // "never", "sometimes", "regularly", "daily"

    [JsonPropertyName("diet")]
    public string? Diet { get; set; } // "anything", "vegetarian", "vegan", "kosher", "halal"

    [JsonPropertyName("pets")]
    public string? Pets { get; set; } // "none", "dog", "cat", "both", "other"

    [JsonPropertyName("children")]
    public string? Children { get; set; } // "none", "have", "want", "open"
}

public class UserPhoto
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}

/// <summary>
/// Unified Date Mark model to replace both DateMark and MarkDate
/// </summary>
public class DateMark
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("placeId")]
    public string? PlaceId { get; set; }

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("qualities")]
    public List<string> Qualities { get; set; } = new();

    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = new();

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("photoReferences")]
    public List<string> PhotoReferences { get; set; } = new();

    [JsonPropertyName("placePhotoUrl")]
    public string? PlacePhotoUrl { get; set; }

    [JsonPropertyName("placePhotoUrls")]
    public List<string> PlacePhotoUrls { get; set; } = new();

    [JsonPropertyName("userPhotoUrl")]
    public string? UserPhotoUrl { get; set; }

    [JsonPropertyName("userPhotoUrls")]
    public List<string> UserPhotoUrls { get; set; } = new();

    [JsonPropertyName("visitDate")]
    public DateTime? VisitDate { get; set; }

    [JsonPropertyName("savedAt")]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("rating")]
    public int? Rating { get; set; } // 1-5 stars

    [JsonPropertyName("wouldRecommend")]
    public bool? WouldRecommend { get; set; }

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "public";
}

/// <summary>
/// Activity statistics for user profiles
/// </summary>
public class ActivityStatistics
{
    [JsonPropertyName("totalDateMarks")]
    public int TotalDateMarks { get; set; }

    [JsonPropertyName("uniqueCategories")]
    public int UniqueCategories { get; set; }

    [JsonPropertyName("uniqueTags")]
    public int UniqueTags { get; set; }

    [JsonPropertyName("uniqueQualities")]
    public int UniqueQualities { get; set; }

    [JsonPropertyName("averageRating")]
    public double AverageRating { get; set; }

    [JsonPropertyName("recommendationRate")]
    public double RecommendationRate { get; set; }
}
