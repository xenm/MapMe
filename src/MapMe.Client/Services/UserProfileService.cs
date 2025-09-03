using System.Net.Http.Json;
using System.Text.Json;
using MapMe.Client.Models;
using Microsoft.JSInterop;

namespace MapMe.Client.Services;

/// <summary>
/// Service for managing user profiles and Date Marks with real data (no fake data)
/// </summary>
public class UserProfileService
{
    private const string ProfileStorageKey = "userProfile";
    private const string DateMarksStorageKey = "dateMarks";
    private const string AllProfilesStorageKey = "allUserProfiles";
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    public UserProfileService(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Retry mechanism for profile loading with exponential backoff
    /// </summary>
    public async Task<UserProfile?> GetCurrentUserProfileWithRetryAsync(int maxRetries = 3)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var profile = await GetCurrentUserProfileAsync();
            if (profile != null)
            {
                return profile;
            }

            // Exponential backoff: wait 1s, 2s, 4s between retries
            if (attempt < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }

        return null;
    }

    /// <summary>
    /// Get the current user's profile - returns null if not available, never fake data
    /// </summary>
    public async Task<UserProfile?> GetCurrentUserProfileAsync()
    {
        try
        {
            // First, try to fetch from server API
            var response = await _httpClient.GetAsync("/api/profile/current");
            if (response.IsSuccessStatusCode)
            {
                var serverProfile = await response.Content.ReadFromJsonAsync<UserProfile>();
                if (serverProfile != null)
                {
                    // Cache in localStorage for offline access
                    var json = JsonSerializer.Serialize(serverProfile);
                    await _jsRuntime.InvokeVoidAsync("MapMe.storage.save", ProfileStorageKey, json);
                    return serverProfile;
                }
            }

            // Fallback to localStorage if server request fails
            var cachedJson = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", ProfileStorageKey);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cachedProfile = JsonSerializer.Deserialize<UserProfile>(cachedJson);
                if (cachedProfile != null)
                {
                    return cachedProfile;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading current user profile: {ex.Message}");
        }

        // Return null - UI components must handle this properly with loading states and retry buttons
        return null;
    }

    /// <summary>
    /// Get a user profile by username
    /// </summary>
    public async Task<UserProfile?> GetUserProfileAsync(string username)
    {
        try
        {
            // First check if it's the current user (by display name only, no hardcoded IDs)
            if (string.Equals(username, "current user", StringComparison.OrdinalIgnoreCase))
            {
                return await GetCurrentUserProfileAsync();
            }

            // Load all profiles and find the requested one
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", AllProfilesStorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var profiles = JsonSerializer.Deserialize<Dictionary<string, UserProfile>>(json);
                if (profiles?.TryGetValue(username.ToLowerInvariant(), out var profile) == true)
                {
                    return profile;
                }
            }

            // If profile doesn't exist, create a basic one
            return new UserProfile
            {
                Id = Guid.NewGuid().ToString(),
                UserId = username.ToLowerInvariant(),
                DisplayName = username,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user profile for {username}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save the current user's profile
    /// </summary>
    public async Task<bool> SaveCurrentUserProfileAsync(UserProfile profile)
    {
        try
        {
            profile.UpdatedAt = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(profile);
            await _jsRuntime.InvokeVoidAsync("MapMe.storage.save", ProfileStorageKey, json);

            // Also save to all profiles collection
            await SaveToAllProfilesAsync(profile);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving current user profile: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save a profile to the all profiles collection
    /// </summary>
    private async Task SaveToAllProfilesAsync(UserProfile profile)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", AllProfilesStorageKey);
            var profiles = new Dictionary<string, UserProfile>();

            if (!string.IsNullOrWhiteSpace(json))
            {
                profiles = JsonSerializer.Deserialize<Dictionary<string, UserProfile>>(json) ?? new();
            }

            profiles[profile.UserId.ToLowerInvariant()] = profile;

            var updatedJson = JsonSerializer.Serialize(profiles);
            await _jsRuntime.InvokeVoidAsync("MapMe.storage.save", AllProfilesStorageKey, updatedJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving to all profiles: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all Date Marks from all users
    /// </summary>
    public async Task<List<DateMark>> GetAllDateMarksAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", DateMarksStorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var allMarks = JsonSerializer.Deserialize<List<DateMark>>(json) ?? new();
                return allMarks.OrderByDescending(m => m.SavedAt).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading all Date Marks: {ex.Message}");
        }

        return new List<DateMark>();
    }

    /// <summary>
    /// Get all Date Marks for a user
    /// </summary>
    public async Task<List<DateMark>> GetUserDateMarksAsync(string userId)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", DateMarksStorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var allMarks = JsonSerializer.Deserialize<List<DateMark>>(json) ?? new();
                return allMarks
                    .Where(m => string.Equals(m.UserId, userId, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(m.CreatedBy, userId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(m => m.SavedAt)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Date Marks for user {userId}: {ex.Message}");
        }

        return new List<DateMark>();
    }

    /// <summary>
    /// Save a Date Mark, checking for duplicates by place ID and user
    /// </summary>
    public async Task<(bool Success, DateMark? ExistingMark)> SaveDateMarkAsync(DateMark dateMark)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", DateMarksStorageKey);
            var allMarks = new List<DateMark>();

            if (!string.IsNullOrWhiteSpace(json))
            {
                allMarks = JsonSerializer.Deserialize<List<DateMark>>(json) ?? new();
            }

            // Check for existing Date Mark at the same place by the same user
            if (!string.IsNullOrWhiteSpace(dateMark.PlaceId))
            {
                var existingMark = allMarks.FirstOrDefault(m =>
                    !string.IsNullOrWhiteSpace(m.PlaceId) &&
                    string.Equals(m.PlaceId, dateMark.PlaceId, StringComparison.OrdinalIgnoreCase) &&
                    (string.Equals(m.UserId, dateMark.UserId, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(m.CreatedBy, dateMark.CreatedBy, StringComparison.OrdinalIgnoreCase)));

                if (existingMark != null)
                {
                    return (false, existingMark);
                }
            }

            // Add new Date Mark
            if (string.IsNullOrWhiteSpace(dateMark.Id))
            {
                dateMark.Id = Guid.NewGuid().ToString();
            }

            dateMark.SavedAt = DateTime.UtcNow;
            dateMark.UpdatedAt = DateTime.UtcNow;

            allMarks.Add(dateMark);

            var updatedJson = JsonSerializer.Serialize(allMarks);
            await _jsRuntime.InvokeVoidAsync("MapMe.storage.save", DateMarksStorageKey, updatedJson);

            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving Date Mark: {ex.Message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Update an existing Date Mark
    /// </summary>
    public async Task<bool> UpdateDateMarkAsync(DateMark dateMark)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", DateMarksStorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var allMarks = JsonSerializer.Deserialize<List<DateMark>>(json) ?? new();
            var existingIndex = allMarks.FindIndex(m => m.Id == dateMark.Id);

            if (existingIndex >= 0)
            {
                dateMark.UpdatedAt = DateTime.UtcNow;
                allMarks[existingIndex] = dateMark;

                var updatedJson = JsonSerializer.Serialize(allMarks);
                await _jsRuntime.InvokeVoidAsync("MapMe.storage.save", DateMarksStorageKey, updatedJson);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Date Mark: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete a Date Mark
    /// </summary>
    public async Task<bool> DeleteDateMarkAsync(string dateMarkId)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", DateMarksStorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var allMarks = JsonSerializer.Deserialize<List<DateMark>>(json) ?? new();
            var markToRemove = allMarks.FirstOrDefault(m => m.Id == dateMarkId);

            if (markToRemove != null)
            {
                allMarks.Remove(markToRemove);

                var updatedJson = JsonSerializer.Serialize(allMarks);
                await _jsRuntime.InvokeVoidAsync("MapMe.storage.save", DateMarksStorageKey, updatedJson);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting Date Mark: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get activity statistics for a user
    /// </summary>
    public async Task<UserActivityStats> GetUserActivityStatsAsync(string userId)
    {
        var dateMarks = await GetUserDateMarksAsync(userId);

        var uniqueCategories = dateMarks.SelectMany(m => m.Categories ?? new List<string>()).Distinct().Count();
        var uniqueTags = dateMarks.SelectMany(m => m.Tags ?? new List<string>()).Distinct().Count();
        var uniqueQualities = dateMarks.SelectMany(m => m.Qualities ?? new List<string>()).Distinct().Count();

        var ratedMarks = dateMarks.Where(m => m.Rating.HasValue && m.Rating.Value > 0).ToList();
        var recommendationMarks = dateMarks.Where(m => m.WouldRecommend.HasValue).ToList();

        return new UserActivityStats
        {
            TotalDateMarks = dateMarks.Count,
            UniqueCategories = uniqueCategories,
            UniqueTags = uniqueTags,
            UniqueQualities = uniqueQualities,
            AverageRating = ratedMarks.Any() ? ratedMarks.Average(m => m.Rating!.Value) : 0.0,
            RecommendationRate = recommendationMarks.Any()
                ? (double)recommendationMarks.Count(m => m.WouldRecommend == true) / recommendationMarks.Count * 100.0
                : 0.0
        };
    }
}

public class UserActivityStats
{
    public int TotalDateMarks { get; set; }
    public int UniqueCategories { get; set; }
    public int UniqueTags { get; set; }
    public int UniqueQualities { get; set; }
    public double AverageRating { get; set; }
    public double RecommendationRate { get; set; }
}