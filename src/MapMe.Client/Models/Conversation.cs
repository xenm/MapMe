using System.Text.Json.Serialization;

namespace MapMe.Client.Models;

/// <summary>
/// Client-side conversation model
/// </summary>
public class Conversation
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("participant1Id")] public string Participant1Id { get; set; } = string.Empty;

    [JsonPropertyName("participant2Id")] public string Participant2Id { get; set; } = string.Empty;

    [JsonPropertyName("lastMessageId")] public string? LastMessageId { get; set; }

    [JsonPropertyName("lastMessageContent")]
    public string? LastMessageContent { get; set; }

    [JsonPropertyName("lastMessageAt")] public DateTime? LastMessageAt { get; set; }

    [JsonPropertyName("lastMessageSenderId")]
    public string? LastMessageSenderId { get; set; }

    [JsonPropertyName("unreadCount1")] public int UnreadCount1 { get; set; }

    [JsonPropertyName("unreadCount2")] public int UnreadCount2 { get; set; }

    [JsonPropertyName("isArchived1")] public bool IsArchived1 { get; set; }

    [JsonPropertyName("isArchived2")] public bool IsArchived2 { get; set; }

    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

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