using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MapMe.Models;

namespace MapMe.Repositories;

/// <summary>
/// In-memory implementation of chat message repository for development and testing
/// </summary>
public sealed class InMemoryChatMessageRepository : IChatMessageRepository
{
    private readonly ConcurrentDictionary<string, ChatMessage> _messages = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _messagesByConversation = new();

    public Task UpsertAsync(ChatMessage message, CancellationToken ct = default)
    {
        _messages[message.Id] = message;
        
        // Add to conversation index
        var conversationMessages = _messagesByConversation.GetOrAdd(message.ConversationId, _ => new ConcurrentBag<string>());
        if (!conversationMessages.Contains(message.Id))
        {
            conversationMessages.Add(message.Id);
        }
        
        return Task.CompletedTask;
    }

    public Task<ChatMessage?> GetByIdAsync(string messageId, CancellationToken ct = default)
    {
        _messages.TryGetValue(messageId, out var message);
        return Task.FromResult(message);
    }

    public async IAsyncEnumerable<ChatMessage> GetByConversationAsync(
        string conversationId,
        int skip = 0,
        int take = 50,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!_messagesByConversation.TryGetValue(conversationId, out var messageIds))
            yield break;

        var messages = messageIds
            .Select(id => _messages.TryGetValue(id, out var msg) ? msg : null)
            .Where(msg => msg != null && !msg.IsDeleted)
            .OrderByDescending(msg => msg!.CreatedAt)
            .Skip(skip)
            .Take(take);

        foreach (var message in messages)
        {
            ct.ThrowIfCancellationRequested();
            yield return message!;
            await Task.Yield();
        }
    }

    public Task MarkAsReadAsync(string conversationId, string userId, CancellationToken ct = default)
    {
        if (!_messagesByConversation.TryGetValue(conversationId, out var messageIds))
            return Task.CompletedTask;

        foreach (var messageId in messageIds)
        {
            if (_messages.TryGetValue(messageId, out var message) && 
                message.ReceiverId == userId && 
                !message.IsRead)
            {
                var updatedMessage = message with 
                { 
                    IsRead = true, 
                    UpdatedAt = DateTimeOffset.UtcNow 
                };
                _messages[messageId] = updatedMessage;
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string messageId, CancellationToken ct = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            var deletedMessage = message with 
            { 
                IsDeleted = true, 
                UpdatedAt = DateTimeOffset.UtcNow 
            };
            _messages[messageId] = deletedMessage;
        }

        return Task.CompletedTask;
    }

    public Task<int> GetUnreadCountAsync(string conversationId, string userId, CancellationToken ct = default)
    {
        if (!_messagesByConversation.TryGetValue(conversationId, out var messageIds))
            return Task.FromResult(0);

        var unreadCount = messageIds
            .Select(id => _messages.TryGetValue(id, out var msg) ? msg : null)
            .Where(msg => msg != null && 
                         !msg.IsDeleted && 
                         msg.ReceiverId == userId && 
                         !msg.IsRead)
            .Count();

        return Task.FromResult(unreadCount);
    }
}
