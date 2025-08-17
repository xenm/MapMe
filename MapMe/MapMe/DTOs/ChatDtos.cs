using System.Text.Json.Serialization;
using MapMe.Models;

namespace MapMe.DTOs;

/// <summary>
/// Request DTO for sending a chat message
/// </summary>
public sealed record SendMessageRequest(
    [property: JsonPropertyName("receiverId")] string ReceiverId,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("messageType")] string MessageType = "text",
    [property: JsonPropertyName("metadata")] MessageMetadataDto? Metadata = null
)
{
    public ChatMessage ToChatMessage(string senderId, DateTimeOffset now)
    {
        var conversationId = Conversation.CreateConversationId(senderId, ReceiverId);
        var messageId = $"msg_{Guid.NewGuid():N}";

        return new ChatMessage(
            Id: messageId,
            ConversationId: conversationId,
            SenderId: senderId,
            ReceiverId: ReceiverId,
            Content: Content,
            MessageType: MessageType,
            Metadata: Metadata?.ToMessageMetadata(),
            IsRead: false,
            IsDelivered: true,
            CreatedAt: now,
            UpdatedAt: now,
            IsDeleted: false
        );
    }
}

/// <summary>
/// DTO for message metadata
/// </summary>
public sealed record MessageMetadataDto(
    [property: JsonPropertyName("imageUrl")] string? ImageUrl = null,
    [property: JsonPropertyName("thumbnailUrl")] string? ThumbnailUrl = null,
    [property: JsonPropertyName("latitude")] double? Latitude = null,
    [property: JsonPropertyName("longitude")] double? Longitude = null,
    [property: JsonPropertyName("locationName")] string? LocationName = null,
    [property: JsonPropertyName("dateMarkId")] string? DateMarkId = null,
    [property: JsonPropertyName("dateMarkName")] string? DateMarkName = null
)
{
    public MessageMetadata ToMessageMetadata() => new(
        ImageUrl: ImageUrl,
        ThumbnailUrl: ThumbnailUrl,
        Latitude: Latitude,
        Longitude: Longitude,
        LocationName: LocationName,
        DateMarkId: DateMarkId,
        DateMarkName: DateMarkName
    );
}

/// <summary>
/// Response DTO for conversation with participant details
/// </summary>
public sealed record ConversationResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("otherParticipant")] UserSummary OtherParticipant,
    [property: JsonPropertyName("lastMessage")] MessageSummary? LastMessage,
    [property: JsonPropertyName("unreadCount")] int UnreadCount,
    [property: JsonPropertyName("isArchived")] bool IsArchived,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTimeOffset UpdatedAt
);

/// <summary>
/// Summary of a user for conversation lists
/// </summary>
public sealed record UserSummary(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("avatarUrl")] string? AvatarUrl
);

/// <summary>
/// Summary of a message for conversation lists
/// </summary>
public sealed record MessageSummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("messageType")] string MessageType,
    [property: JsonPropertyName("senderId")] string SenderId,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("isRead")] bool IsRead
);

/// <summary>
/// Request DTO for marking messages as read
/// </summary>
public sealed record MarkAsReadRequest(
    [property: JsonPropertyName("conversationId")] string ConversationId
);

/// <summary>
/// Request DTO for archiving/unarchiving a conversation
/// </summary>
public sealed record ArchiveConversationRequest(
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("isArchived")] bool IsArchived
);
