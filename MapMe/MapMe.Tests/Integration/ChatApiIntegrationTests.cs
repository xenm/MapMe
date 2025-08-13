using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using Xunit;

namespace MapMe.Tests.Integration;

/// <summary>
/// Integration tests for Chat API endpoints
/// </summary>
public class ChatApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ChatApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace repositories with in-memory implementations for testing
                // Use Singleton instead of Scoped to ensure data persists between HTTP requests
                services.AddSingleton<IChatMessageRepository, InMemoryChatMessageRepository>();
                services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
                services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
            });
        });
        _client = _factory.CreateClient();
    }

    private async Task SetupTestUsersAsync()
    {
        // Create test user profiles that the chat API expects
        var userProfiles = new[]
        {
            new { UserId = "user1", DisplayName = "Test User 1" },
            new { UserId = "user2", DisplayName = "Test User 2" },
            new { UserId = "user3", DisplayName = "Test User 3" },
            new { UserId = "current_user", DisplayName = "Current User" }
        };

        foreach (var user in userProfiles)
        {
            var profileRequest = new CreateProfileRequest(
                Id: Guid.NewGuid().ToString(),
                UserId: user.UserId,
                DisplayName: user.DisplayName,
                Bio: "Test bio",
                Photos: null,
                PreferredCategories: null,
                Visibility: "public"
            );

            await _client.PostAsJsonAsync("/api/profiles", profileRequest);
        }
    }

    [Fact]
    public async Task SendMessage_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await SetupTestUsersAsync();

        var request = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Hello there!",
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/messages", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<ChatMessage>();
        Assert.NotNull(message);
        Assert.Equal("user1", message.SenderId);
        Assert.Equal("user2", message.ReceiverId);
        Assert.Equal("Hello there!", message.Content);
        Assert.Equal("text", message.MessageType);
    }

    [Fact]
    public async Task SendMessage_MissingUserId_UsesDefaultUser()
    {
        // Arrange
        await SetupTestUsersAsync();

        var request = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Hello there!",
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        // No X-User-Id header - should use default "current_user"

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/messages", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<ChatMessage>();
        Assert.NotNull(message);
        Assert.Equal("current_user", message.SenderId);
    }

    [Fact]
    public async Task SendMessage_EmptyContent_ReturnsBadRequest()
    {
        // Arrange
        await SetupTestUsersAsync();

        var request = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "", // Empty content should be rejected
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/messages", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_NonExistentReceiver_ReturnsBadRequest()
    {
        // Arrange
        await SetupTestUsersAsync();

        var request = new SendMessageRequest(
            ReceiverId: "nonexistent_user",
            Content: "Hello there!",
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/messages", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetConversations_ValidUser_ReturnsConversations()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // First send a message to create a conversation
        var sendRequest = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Hello!",
            MessageType: "text",
            Metadata: null
        );
        await _client.PostAsJsonAsync("/api/chat/messages", sendRequest);

        // Act
        var response = await _client.GetAsync("/api/chat/conversations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversations = await response.Content.ReadFromJsonAsync<List<ConversationResponse>>();
        Assert.NotNull(conversations);
        Assert.Single(conversations);
        Assert.Equal("user2", conversations[0].OtherParticipant.UserId);
    }

    [Fact]
    public async Task GetMessages_ValidConversation_ReturnsMessages()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Send a message to create conversation
        var sendRequest = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Test message",
            MessageType: "text",
            Metadata: null
        );
        var sendResponse = await _client.PostAsJsonAsync("/api/chat/messages", sendRequest);
        var sentMessage = await sendResponse.Content.ReadFromJsonAsync<ChatMessage>();

        // Act
        var response = await _client.GetAsync($"/api/chat/conversations/{sentMessage!.ConversationId}/messages");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessage>>();
        Assert.NotNull(messages);
        Assert.Single(messages);
        Assert.Equal("Test message", messages[0].Content);
    }

    [Fact]
    public async Task GetMessages_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Send multiple messages
        var conversationId = "";
        for (int i = 1; i <= 5; i++)
        {
            var sendRequest = new SendMessageRequest(
                ReceiverId: "user2",
                Content: $"Message {i}",
                MessageType: "text",
                Metadata: null
            );
            var sendResponse = await _client.PostAsJsonAsync("/api/chat/messages", sendRequest);
            if (i == 1)
            {
                var message = await sendResponse.Content.ReadFromJsonAsync<ChatMessage>();
                conversationId = message!.ConversationId;
            }
        }

        // Act
        var response = await _client.GetAsync($"/api/chat/conversations/{conversationId}/messages?skip=2&take=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessage>>();
        Assert.NotNull(messages);
        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public async Task MarkAsRead_ValidRequest_ReturnsOk()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Send a message first
        var sendRequest = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Test message",
            MessageType: "text",
            Metadata: null
        );
        var sendResponse = await _client.PostAsJsonAsync("/api/chat/messages", sendRequest);
        var message = await sendResponse.Content.ReadFromJsonAsync<ChatMessage>();

        var markReadRequest = new MarkAsReadRequest(ConversationId: message!.ConversationId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/messages/read", markReadRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Note: This endpoint returns empty OK response, not a ChatMessage
    }

    [Fact]
    public async Task ArchiveConversation_ValidRequest_ReturnsOk()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Send a message first
        var sendRequest = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Test message",
            MessageType: "text",
            Metadata: null
        );
        var sendResponse = await _client.PostAsJsonAsync("/api/chat/messages", sendRequest);
        var message = await sendResponse.Content.ReadFromJsonAsync<ChatMessage>();

        var archiveRequest = new ArchiveConversationRequest(
            ConversationId: message!.ConversationId,
            IsArchived: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/conversations/archive", archiveRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Note: This endpoint returns empty OK response, not a ChatMessage
    }

    [Fact]
    public async Task DeleteMessage_ValidRequest_ReturnsOk()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Send a message first
        var sendRequest = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Test message to delete",
            MessageType: "text",
            Metadata: null
        );
        var sendResponse = await _client.PostAsJsonAsync("/api/chat/messages", sendRequest);
        var message = await sendResponse.Content.ReadFromJsonAsync<ChatMessage>();

        // Act
        var response = await _client.DeleteAsync($"/api/chat/messages/{message!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Note: This endpoint returns empty OK response, not a ChatMessage
    }

    [Fact]
    public async Task GetConversations_MultipleUsers_ReturnsCorrectConversations()
    {
        // Arrange - User1 sends messages to User2 and User3
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Send to user2
        await _client.PostAsJsonAsync("/api/chat/messages", new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Hello user2",
            MessageType: "text",
            Metadata: null
        ));

        // Send to user3
        await _client.PostAsJsonAsync("/api/chat/messages", new SendMessageRequest(
            ReceiverId: "user3",
            Content: "Hello user3",
            MessageType: "text",
            Metadata: null
        ));

        // Act
        var response = await _client.GetAsync("/api/chat/conversations");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversations = await response.Content.ReadFromJsonAsync<List<ConversationResponse>>();
        Assert.NotNull(conversations);
        Assert.Equal(2, conversations.Count);
        
        var userIds = conversations.Select(c => c.OtherParticipant.UserId).ToList();
        Assert.Contains("user2", userIds);
        Assert.Contains("user3", userIds);
    }

    [Fact]
    public async Task GetMessages_NonExistentConversation_ReturnsEmptyList()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        // Act
        var response = await _client.GetAsync("/api/chat/conversations/nonexistent_conv/messages");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessage>>();
        Assert.NotNull(messages);
        Assert.Empty(messages);
    }
}
