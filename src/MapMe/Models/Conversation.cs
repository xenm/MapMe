using System.Text.Json.Serialization;

namespace MapMe.Models;

/// <summary>
/// Represents a conversation between two users
/// </summary>
public sealed record Conversation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("participant1Id")]
    string Participant1Id,
    [property: JsonPropertyName("participant2Id")]
    string Participant2Id,
    [property: JsonPropertyName("lastMessageId")]
    string? LastMessageId,
    [property: JsonPropertyName("lastMessageContent")]
    string? LastMessageContent,
    [property: JsonPropertyName("lastMessageAt")]
    DateTimeOffset? LastMessageAt,
    [property: JsonPropertyName("lastMessageSenderId")]
    string? LastMessageSenderId,
    [property: JsonPropertyName("unreadCount1")]
    int UnreadCount1, // Unread count for participant1
    [property: JsonPropertyName("unreadCount2")]
    int UnreadCount2, // Unread count for participant2
    [property: JsonPropertyName("isArchived1")]
    bool IsArchived1, // Archived status for participant1
    [property: JsonPropertyName("isArchived2")]
    bool IsArchived2, // Archived status for participant2
    [property: JsonPropertyName("createdAt")]
    DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updatedAt")]
    DateTimeOffset UpdatedAt
)
{
    /// <summary>
    /// Gets the other participant's ID given one participant's ID
    /// </summary>
    public string GetOtherParticipantId(string userId) =>
        userId == Participant1Id ? Participant2Id : Participant1Id;

    /// <summary>
    /// Gets the unread count for a specific user
    /// </summary>
    public int GetUnreadCountForUser(string userId) =>
        userId == Participant1Id ? UnreadCount1 : UnreadCount2;

    /// <summary>
    /// Gets the archived status for a specific user
    /// </summary>
    public bool IsArchivedForUser(string userId) =>
        userId == Participant1Id ? IsArchived1 : IsArchived2;

    /// <summary>
    /// Creates a conversation ID from two user IDs (deterministic ordering)
    /// </summary>
    public static string CreateConversationId(string userId1, string userId2)
    {
        var orderedIds = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        return $"conv_{orderedIds[0]}_{orderedIds[1]}";
    }
}