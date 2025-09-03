using System.Text.Json.Serialization;

namespace MapMe.Models;

/// <summary>
/// Represents a chat message between users
/// </summary>
public sealed record ChatMessage(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("conversationId")]
    string ConversationId,
    [property: JsonPropertyName("senderId")]
    string SenderId,
    [property: JsonPropertyName("receiverId")]
    string ReceiverId,
    [property: JsonPropertyName("content")]
    string Content,
    [property: JsonPropertyName("messageType")]
    string MessageType, // "text", "image", "location", "datemark"
    [property: JsonPropertyName("metadata")]
    MessageMetadata? Metadata,
    [property: JsonPropertyName("isRead")] bool IsRead,
    [property: JsonPropertyName("isDelivered")]
    bool IsDelivered,
    [property: JsonPropertyName("createdAt")]
    DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updatedAt")]
    DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("isDeleted")]
    bool IsDeleted
);

/// <summary>
/// Additional metadata for different message types
/// </summary>
public sealed record MessageMetadata(
    [property: JsonPropertyName("imageUrl")]
    string? ImageUrl,
    [property: JsonPropertyName("thumbnailUrl")]
    string? ThumbnailUrl,
    [property: JsonPropertyName("latitude")]
    double? Latitude,
    [property: JsonPropertyName("longitude")]
    double? Longitude,
    [property: JsonPropertyName("locationName")]
    string? LocationName,
    [property: JsonPropertyName("dateMarkId")]
    string? DateMarkId,
    [property: JsonPropertyName("dateMarkName")]
    string? DateMarkName
);