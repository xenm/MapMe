using MapMe.Models;
using MapMe.Repositories;
using Xunit;

namespace MapMe.Tests.Unit;

/// <summary>
/// Unit tests for Chat repository implementations
/// </summary>
public class ChatRepositoryTests
{
    [Fact]
    public async Task ChatMessageRepository_UpsertAndGet_WorksCorrectly()
    {
        // Arrange
        var repository = new InMemoryChatMessageRepository();
        var message = new ChatMessage(
            Id: "msg1",
            ConversationId: "conv1",
            SenderId: "user1",
            ReceiverId: "user2",
            Content: "Hello!",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            IsDeleted: false
        );

        // Act
        await repository.UpsertAsync(message);
        var retrieved = await repository.GetByIdAsync("msg1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("msg1", retrieved.Id);
        Assert.Equal("Hello!", retrieved.Content);
        Assert.Equal("user1", retrieved.SenderId);
        Assert.Equal("user2", retrieved.ReceiverId);
    }

    [Fact]
    public async Task ChatMessageRepository_GetByConversation_ReturnsCorrectMessages()
    {
        // Arrange
        var repository = new InMemoryChatMessageRepository();
        var baseTime = DateTimeOffset.UtcNow;

        var message1 = new ChatMessage(
            Id: "msg1",
            ConversationId: "conv1",
            SenderId: "user1",
            ReceiverId: "user2",
            Content: "First message",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: baseTime.AddMinutes(-2),
            UpdatedAt: baseTime.AddMinutes(-2),
            IsDeleted: false
        );

        var message2 = new ChatMessage(
            Id: "msg2",
            ConversationId: "conv1",
            SenderId: "user2",
            ReceiverId: "user1",
            Content: "Second message",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: baseTime.AddMinutes(-1),
            UpdatedAt: baseTime.AddMinutes(-1),
            IsDeleted: false
        );

        var message3 = new ChatMessage(
            Id: "msg3",
            ConversationId: "conv2", // Different conversation
            SenderId: "user1",
            ReceiverId: "user3",
            Content: "Different conversation",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: baseTime,
            UpdatedAt: baseTime,
            IsDeleted: false
        );

        await repository.UpsertAsync(message1);
        await repository.UpsertAsync(message2);
        await repository.UpsertAsync(message3);

        // Act
        var messages = new List<ChatMessage>();
        await foreach (var msg in repository.GetByConversationAsync("conv1"))
        {
            messages.Add(msg);
        }

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal("conv1", m.ConversationId));
        
        // Should be ordered by CreatedAt descending (newest first)
        Assert.Equal("msg2", messages[0].Id);
        Assert.Equal("msg1", messages[1].Id);
    }

    [Fact]
    public async Task ChatMessageRepository_MarkAsRead_UpdatesCorrectMessages()
    {
        // Arrange
        var repository = new InMemoryChatMessageRepository();
        
        var message1 = new ChatMessage(
            Id: "msg1",
            ConversationId: "conv1",
            SenderId: "user1",
            ReceiverId: "user2",
            Content: "Message 1",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            IsDeleted: false
        );

        var message2 = new ChatMessage(
            Id: "msg2",
            ConversationId: "conv1",
            SenderId: "user1",
            ReceiverId: "user2",
            Content: "Message 2",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            IsDeleted: false
        );

        await repository.UpsertAsync(message1);
        await repository.UpsertAsync(message2);

        // Act
        await repository.MarkAsReadAsync("conv1", "user2");

        // Assert
        var updatedMessage1 = await repository.GetByIdAsync("msg1");
        var updatedMessage2 = await repository.GetByIdAsync("msg2");
        
        Assert.True(updatedMessage1!.IsRead);
        Assert.True(updatedMessage2!.IsRead);
    }

    [Fact]
    public async Task ChatMessageRepository_GetUnreadCount_ReturnsCorrectCount()
    {
        // Arrange
        var repository = new InMemoryChatMessageRepository();
        
        // Add read message
        var readMessage = new ChatMessage(
            Id: "msg1",
            ConversationId: "conv1",
            SenderId: "user1",
            ReceiverId: "user2",
            Content: "Read message",
            MessageType: "text",
            Metadata: null,
            IsRead: true,
            IsDelivered: true,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-3),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-3),
            IsDeleted: false
        );

        // Add unread messages
        var unreadMessage1 = new ChatMessage(
            Id: "msg2",
            ConversationId: "conv1",
            SenderId: "user1",
            ReceiverId: "user2",
            Content: "Unread message 1",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            IsDeleted: false
        );

        var unreadMessage2 = new ChatMessage(
            Id: "msg3",
            ConversationId: "conv1",
            SenderId: "user1",
            ReceiverId: "user2",
            Content: "Unread message 2",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            IsDeleted: false
        );

        await repository.UpsertAsync(readMessage);
        await repository.UpsertAsync(unreadMessage1);
        await repository.UpsertAsync(unreadMessage2);

        // Act
        var unreadCount = await repository.GetUnreadCountAsync("conv1", "user2");

        // Assert
        Assert.Equal(2, unreadCount);
    }

    [Fact]
    public async Task ChatMessageRepository_Delete_MarksAsDeleted()
    {
        // Arrange
        var repository = new InMemoryChatMessageRepository();
        var message = new ChatMessage(
            Id: "msg1",
            ConversationId: "conv1",
            SenderId: "user1",
            ReceiverId: "user2",
            Content: "To be deleted",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            IsDeleted: false
        );

        await repository.UpsertAsync(message);

        // Act
        await repository.DeleteAsync("msg1");

        // Assert
        var deletedMessage = await repository.GetByIdAsync("msg1");
        Assert.NotNull(deletedMessage);
        Assert.True(deletedMessage.IsDeleted);
    }

    [Fact]
    public async Task ConversationRepository_UpsertAndGet_WorksCorrectly()
    {
        // Arrange
        var repository = new InMemoryConversationRepository();
        var conversation = new Conversation(
            Id: "conv1",
            Participant1Id: "user1",
            Participant2Id: "user2",
            LastMessageId: null,
            LastMessageContent: null,
            LastMessageAt: null,
            LastMessageSenderId: null,
            UnreadCount1: 0,
            UnreadCount2: 0,
            IsArchived1: false,
            IsArchived2: false,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        // Act
        await repository.UpsertAsync(conversation);
        var retrieved = await repository.GetByIdAsync("conv1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("conv1", retrieved.Id);
        Assert.Equal("user1", retrieved.Participant1Id);
        Assert.Equal("user2", retrieved.Participant2Id);
    }

    [Fact]
    public async Task ConversationRepository_GetOrCreateConversation_CreatesNewWhenNotExists()
    {
        // Arrange
        var repository = new InMemoryConversationRepository();

        // Act
        var conversation = await repository.GetOrCreateConversationAsync("user1", "user2");

        // Assert
        Assert.NotNull(conversation);
        Assert.Equal("conv_user1_user2", conversation.Id);
        Assert.Equal("user1", conversation.Participant1Id);
        Assert.Equal("user2", conversation.Participant2Id);
    }

    [Fact]
    public async Task ConversationRepository_GetOrCreateConversation_ReturnsExistingWhenExists()
    {
        // Arrange
        var repository = new InMemoryConversationRepository();
        var originalTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        
        var existingConversation = new Conversation(
            Id: "conv_user1_user2",
            Participant1Id: "user1",
            Participant2Id: "user2",
            LastMessageId: "msg1",
            LastMessageContent: "Existing message",
            LastMessageAt: originalTime,
            LastMessageSenderId: "user1",
            UnreadCount1: 0,
            UnreadCount2: 1,
            IsArchived1: false,
            IsArchived2: false,
            CreatedAt: originalTime,
            UpdatedAt: originalTime
        );

        await repository.UpsertAsync(existingConversation);

        // Act
        var conversation = await repository.GetOrCreateConversationAsync("user1", "user2");

        // Assert
        Assert.NotNull(conversation);
        Assert.Equal("conv_user1_user2", conversation.Id);
        Assert.Equal("Existing message", conversation.LastMessageContent);
        Assert.Equal(1, conversation.UnreadCount2);
    }

    [Fact]
    public async Task ConversationRepository_GetByUser_ReturnsUserConversations()
    {
        // Arrange
        var repository = new InMemoryConversationRepository();
        
        var conv1 = new Conversation(
            Id: "conv_user1_user2",
            Participant1Id: "user1",
            Participant2Id: "user2",
            LastMessageId: null,
            LastMessageContent: null,
            LastMessageAt: null,
            LastMessageSenderId: null,
            UnreadCount1: 0,
            UnreadCount2: 0,
            IsArchived1: false,
            IsArchived2: false,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-2)
        );

        var conv2 = new Conversation(
            Id: "conv_user1_user3",
            Participant1Id: "user1",
            Participant2Id: "user3",
            LastMessageId: null,
            LastMessageContent: null,
            LastMessageAt: null,
            LastMessageSenderId: null,
            UnreadCount1: 0,
            UnreadCount2: 0,
            IsArchived1: false,
            IsArchived2: false,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-1)
        );

        var conv3 = new Conversation(
            Id: "conv_user2_user3",
            Participant1Id: "user2",
            Participant2Id: "user3",
            LastMessageId: null,
            LastMessageContent: null,
            LastMessageAt: null,
            LastMessageSenderId: null,
            UnreadCount1: 0,
            UnreadCount2: 0,
            IsArchived1: false,
            IsArchived2: false,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        await repository.UpsertAsync(conv1);
        await repository.UpsertAsync(conv2);
        await repository.UpsertAsync(conv3);

        // Act
        var conversations = new List<Conversation>();
        await foreach (var conv in repository.GetByUserAsync("user1"))
        {
            conversations.Add(conv);
        }

        // Assert
        Assert.Equal(2, conversations.Count);
        Assert.Contains(conversations, c => c.Id == "conv_user1_user2");
        Assert.Contains(conversations, c => c.Id == "conv_user1_user3");
        Assert.DoesNotContain(conversations, c => c.Id == "conv_user2_user3");
    }

    [Fact]
    public async Task ConversationRepository_UpdateConversationMetadata_UpdatesCorrectly()
    {
        // Arrange
        var repository = new InMemoryConversationRepository();
        var conversation = await repository.GetOrCreateConversationAsync("user1", "user2");
        var updateTime = DateTimeOffset.UtcNow;

        // Act
        await repository.UpdateConversationMetadataAsync(
            conversation.Id,
            "msg123",
            "Latest message content",
            "user1",
            updateTime
        );

        // Assert
        var updated = await repository.GetByIdAsync(conversation.Id);
        Assert.NotNull(updated);
        Assert.Equal("msg123", updated.LastMessageId);
        Assert.Equal("Latest message content", updated.LastMessageContent);
        Assert.Equal("user1", updated.LastMessageSenderId);
        Assert.Equal(updateTime, updated.LastMessageAt);
    }

    [Fact]
    public async Task ConversationRepository_SetArchiveStatus_UpdatesCorrectly()
    {
        // Arrange
        var repository = new InMemoryConversationRepository();
        var conversation = await repository.GetOrCreateConversationAsync("user1", "user2");

        // Act
        await repository.SetArchiveStatusAsync(conversation.Id, "user1", true);

        // Assert
        var updated = await repository.GetByIdAsync(conversation.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsArchived1);
        Assert.False(updated.IsArchived2);
    }

    [Fact]
    public async Task ConversationRepository_UpdateUnreadCount_UpdatesCorrectly()
    {
        // Arrange
        var repository = new InMemoryConversationRepository();
        var conversation = await repository.GetOrCreateConversationAsync("user1", "user2");

        // Act
        await repository.UpdateUnreadCountAsync(conversation.Id, "user2", 5);

        // Assert
        var updated = await repository.GetByIdAsync(conversation.Id);
        Assert.NotNull(updated);
        Assert.Equal(0, updated.UnreadCount1);
        Assert.Equal(5, updated.UnreadCount2);
    }

    [Fact]
    public void Conversation_HelperMethods_WorkCorrectly()
    {
        // Arrange
        var conversation = new Conversation(
            Id: "conv1",
            Participant1Id: "user1",
            Participant2Id: "user2",
            LastMessageId: null,
            LastMessageContent: null,
            LastMessageAt: null,
            LastMessageSenderId: null,
            UnreadCount1: 3,
            UnreadCount2: 7,
            IsArchived1: true,
            IsArchived2: false,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow
        );

        // Act & Assert
        Assert.Equal("user2", conversation.GetOtherParticipantId("user1"));
        Assert.Equal("user1", conversation.GetOtherParticipantId("user2"));
        
        Assert.Equal(3, conversation.GetUnreadCountForUser("user1"));
        Assert.Equal(7, conversation.GetUnreadCountForUser("user2"));
        
        Assert.True(conversation.IsArchivedForUser("user1"));
        Assert.False(conversation.IsArchivedForUser("user2"));
    }

    [Fact]
    public void Conversation_CreateConversationId_GeneratesConsistentId()
    {
        // Act
        var id1 = Conversation.CreateConversationId("user1", "user2");
        var id2 = Conversation.CreateConversationId("user2", "user1");

        // Assert
        Assert.Equal(id1, id2);
        Assert.Equal("conv_user1_user2", id1);
    }
}
