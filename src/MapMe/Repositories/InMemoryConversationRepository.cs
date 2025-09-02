using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MapMe.Models;

namespace MapMe.Repositories;

/// <summary>
/// In-memory implementation of conversation repository for development and testing
/// </summary>
public sealed class InMemoryConversationRepository : IConversationRepository
{
    private readonly ConcurrentDictionary<string, Conversation> _conversations = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _conversationsByUser = new();

    public Task UpsertAsync(Conversation conversation, CancellationToken ct = default)
    {
        _conversations[conversation.Id] = conversation;

        // Add to user indexes
        var user1Conversations =
            _conversationsByUser.GetOrAdd(conversation.Participant1Id, _ => new ConcurrentBag<string>());
        var user2Conversations =
            _conversationsByUser.GetOrAdd(conversation.Participant2Id, _ => new ConcurrentBag<string>());

        if (!user1Conversations.Contains(conversation.Id))
            user1Conversations.Add(conversation.Id);
        if (!user2Conversations.Contains(conversation.Id))
            user2Conversations.Add(conversation.Id);

        return Task.CompletedTask;
    }

    public Task<Conversation?> GetByIdAsync(string conversationId, CancellationToken ct = default)
    {
        _conversations.TryGetValue(conversationId, out var conversation);
        return Task.FromResult(conversation);
    }

    public async Task<Conversation> GetOrCreateConversationAsync(string userId1, string userId2,
        CancellationToken ct = default)
    {
        var conversationId = Conversation.CreateConversationId(userId1, userId2);

        if (_conversations.TryGetValue(conversationId, out var existingConversation))
        {
            return existingConversation;
        }

        // Create new conversation with deterministic participant ordering
        var orderedIds = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        var now = DateTimeOffset.UtcNow;

        var newConversation = new Conversation(
            Id: conversationId,
            Participant1Id: orderedIds[0],
            Participant2Id: orderedIds[1],
            LastMessageId: null,
            LastMessageContent: null,
            LastMessageAt: null,
            LastMessageSenderId: null,
            UnreadCount1: 0,
            UnreadCount2: 0,
            IsArchived1: false,
            IsArchived2: false,
            CreatedAt: now,
            UpdatedAt: now
        );

        await UpsertAsync(newConversation, ct);
        return newConversation;
    }

    public async IAsyncEnumerable<Conversation> GetByUserAsync(
        string userId,
        bool includeArchived = false,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_conversationsByUser.TryGetValue(userId, out var conversationIds))
            yield break;

        var conversations = conversationIds
            .Select(id => _conversations.TryGetValue(id, out var conv) ? conv : null)
            .Where(conv => conv != null && (includeArchived || !conv.IsArchivedForUser(userId)))
            .OrderByDescending(conv => conv!.LastMessageAt ?? conv.CreatedAt);

        foreach (var conversation in conversations)
        {
            ct.ThrowIfCancellationRequested();
            yield return conversation!;
            await Task.Yield();
        }
    }

    public Task UpdateConversationMetadataAsync(
        string conversationId,
        string lastMessageId,
        string lastMessageContent,
        string lastMessageSenderId,
        DateTimeOffset lastMessageAt,
        CancellationToken ct = default)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            var updatedConversation = conversation with
            {
                LastMessageId = lastMessageId,
                LastMessageContent = lastMessageContent,
                LastMessageSenderId = lastMessageSenderId,
                LastMessageAt = lastMessageAt,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _conversations[conversationId] = updatedConversation;
        }

        return Task.CompletedTask;
    }

    public Task SetArchiveStatusAsync(string conversationId, string userId, bool isArchived,
        CancellationToken ct = default)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            var updatedConversation = userId == conversation.Participant1Id
                ? conversation with { IsArchived1 = isArchived, UpdatedAt = DateTimeOffset.UtcNow }
                : conversation with { IsArchived2 = isArchived, UpdatedAt = DateTimeOffset.UtcNow };

            _conversations[conversationId] = updatedConversation;
        }

        return Task.CompletedTask;
    }

    public Task UpdateUnreadCountAsync(string conversationId, string userId, int unreadCount,
        CancellationToken ct = default)
    {
        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            var updatedConversation = userId == conversation.Participant1Id
                ? conversation with { UnreadCount1 = unreadCount, UpdatedAt = DateTimeOffset.UtcNow }
                : conversation with { UnreadCount2 = unreadCount, UpdatedAt = DateTimeOffset.UtcNow };

            _conversations[conversationId] = updatedConversation;
        }

        return Task.CompletedTask;
    }
}