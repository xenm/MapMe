using MapMe.Models;

namespace MapMe.Repositories;

/// <summary>
/// Repository interface for chat message operations
/// </summary>
public interface IChatMessageRepository
{
    /// <summary>
    /// Create or update a chat message
    /// </summary>
    Task UpsertAsync(ChatMessage message, CancellationToken ct = default);

    /// <summary>
    /// Get a chat message by ID
    /// </summary>
    Task<ChatMessage?> GetByIdAsync(string messageId, CancellationToken ct = default);

    /// <summary>
    /// Get messages for a conversation with pagination
    /// </summary>
    IAsyncEnumerable<ChatMessage> GetByConversationAsync(
        string conversationId,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Mark messages as read for a user in a conversation
    /// </summary>
    Task MarkAsReadAsync(string conversationId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Delete a message (soft delete)
    /// </summary>
    Task DeleteAsync(string messageId, CancellationToken ct = default);

    /// <summary>
    /// Get unread message count for a user in a conversation
    /// </summary>
    Task<int> GetUnreadCountAsync(string conversationId, string userId, CancellationToken ct = default);
}
