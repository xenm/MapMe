using System.Text.Json.Serialization;

namespace MapMe.Client.Models;

/// <summary>
/// Client-side chat message model
/// </summary>
public class ChatMessage
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("conversationId")] public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("senderId")] public string SenderId { get; set; } = string.Empty;

    [JsonPropertyName("receiverId")] public string ReceiverId { get; set; } = string.Empty;

    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "text"; // "text", "image", "location", "datemark"

    [JsonPropertyName("metadata")] public MessageMetadata? Metadata { get; set; }

    [JsonPropertyName("isRead")] public bool IsRead { get; set; }

    [JsonPropertyName("isDelivered")] public bool IsDelivered { get; set; }

    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("isDeleted")] public bool IsDeleted { get; set; }
}

/// <summary>
/// Client-side message metadata
/// </summary>
public class MessageMetadata
{
    [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }

    [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("latitude")] public double? Latitude { get; set; }

    [JsonPropertyName("longitude")] public double? Longitude { get; set; }

    [JsonPropertyName("locationName")] public string? LocationName { get; set; }

    [JsonPropertyName("dateMarkId")] public string? DateMarkId { get; set; }

    [JsonPropertyName("dateMarkName")] public string? DateMarkName { get; set; }
}