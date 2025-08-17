using MapMe.Models;

namespace MapMe.Repositories;

/// <summary>
/// Repository interface for conversation operations
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Create or update a conversation
    /// </summary>
    Task UpsertAsync(Conversation conversation, CancellationToken ct = default);

    /// <summary>
    /// Get a conversation by ID
    /// </summary>
    Task<Conversation?> GetByIdAsync(string conversationId, CancellationToken ct = default);

    /// <summary>
    /// Get or create a conversation between two users
    /// </summary>
    Task<Conversation> GetOrCreateConversationAsync(string userId1, string userId2, CancellationToken ct = default);

    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    IAsyncEnumerable<Conversation> GetByUserAsync(string userId, bool includeArchived = false, CancellationToken ct = default);

    /// <summary>
    /// Update conversation metadata (last message, unread counts, etc.)
    /// </summary>
    Task UpdateConversationMetadataAsync(
        string conversationId,
        string lastMessageId,
        string lastMessageContent,
        string lastMessageSenderId,
        DateTimeOffset lastMessageAt,
        CancellationToken ct = default);

    /// <summary>
    /// Archive/unarchive a conversation for a specific user
    /// </summary>
    Task SetArchiveStatusAsync(string conversationId, string userId, bool isArchived, CancellationToken ct = default);

    /// <summary>
    /// Update unread count for a user in a conversation
    /// </summary>
    Task UpdateUnreadCountAsync(string conversationId, string userId, int unreadCount, CancellationToken ct = default);
}
