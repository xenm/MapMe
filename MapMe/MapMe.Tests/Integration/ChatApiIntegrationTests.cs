using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using MapMe.Services;
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
                // Remove any existing repository registrations (like API Smoke tests)
                var repoDescriptors = services.Where(d => 
                    d.ServiceType == typeof(IUserProfileRepository) ||
                    d.ServiceType == typeof(IDateMarkByUserRepository) ||
                    d.ServiceType == typeof(IChatMessageRepository) ||
                    d.ServiceType == typeof(IConversationRepository))
                    .ToList();
                
                foreach (var descriptor in repoDescriptors)
                {
                    services.Remove(descriptor);
                }
                
                // Register in-memory implementations for testing (using Singleton for data persistence)
                services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
                services.AddSingleton<IDateMarkByUserRepository, InMemoryDateMarkByUserRepository>();
                services.AddSingleton<IChatMessageRepository, InMemoryChatMessageRepository>();
                services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
                
                // Override authentication service for testing
                var authDescriptors = services.Where(d => d.ServiceType == typeof(IAuthenticationService)).ToList();
                foreach (var descriptor in authDescriptors)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<IAuthenticationService, TestAuthenticationService>();
            });
        });
        _client = _factory.CreateClient();
    }

    private async Task SetupTestUsersAsync()
    {
        // Directly populate the in-memory user profile repository to bypass API issues
        // Get the repository instance from the service provider
        using var scope = _factory.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
        
        // Create test user profiles that the chat API expects
        var testUsers = new[]
        {
            new UserProfile(
                Id: "profile_test_user_id",
                UserId: "test_user_id",
                DisplayName: "Test User",
                Bio: "Test bio",
                Photos: Array.Empty<UserPhoto>(),
                Preferences: new UserPreferences(Array.Empty<string>()),
                Visibility: "public",
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: DateTimeOffset.UtcNow
            ),
            new UserProfile(
                Id: "profile_test_user_2",
                UserId: "test_user_2",
                DisplayName: "Test User 2",
                Bio: "Test bio",
                Photos: Array.Empty<UserPhoto>(),
                Preferences: new UserPreferences(Array.Empty<string>()),
                Visibility: "public",
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: DateTimeOffset.UtcNow
            ),
            new UserProfile(
                Id: "profile_test_user_3",
                UserId: "test_user_3",
                DisplayName: "Test User 3",
                Bio: "Test bio",
                Photos: Array.Empty<UserPhoto>(),
                Preferences: new UserPreferences(Array.Empty<string>()),
                Visibility: "public",
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: DateTimeOffset.UtcNow
            )
        };

        // Add users directly to the repository using UpsertAsync
        foreach (var user in testUsers)
        {
            await userRepo.UpsertAsync(user);
        }
    }

    [Fact]
    public async Task SendMessage_ValidRequest_ReturnsCreated()
    {
        // Arrange
        await SetupTestUsersAsync();

        var request = new SendMessageRequest(
            ReceiverId: "test_user_2",
            Content: "Hello there!",
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/messages", request);

        // Debug: Log response content if not successful
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected Created but got {response.StatusCode}. Response: {errorContent}");
        }

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<ChatMessage>();
        Assert.NotNull(message);
        Assert.Equal("test_user_id", message.SenderId);
        Assert.Equal("test_user_2", message.ReceiverId);
        Assert.Equal("Hello there!", message.Content);
        Assert.Equal("text", message.MessageType);
    }

    [Fact]
    public async Task SendMessage_MissingUserId_UsesDefaultUser()
    {
        // Arrange
        await SetupTestUsersAsync();

        var request = new SendMessageRequest(
            ReceiverId: "test_user_2",
            Content: "Hello there!",
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");
        // Using test authentication token

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/messages", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<ChatMessage>();
        Assert.NotNull(message);
        Assert.Equal("test_user_id", message.SenderId);
    }

    [Fact]
    public async Task SendMessage_EmptyContent_ReturnsBadRequest()
    {
        // Arrange
        await SetupTestUsersAsync();

        var request = new SendMessageRequest(
            ReceiverId: "test_user_2",
            Content: "", // Empty content should be rejected
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

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
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

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
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // First send a message to create a conversation
        var sendRequest = new SendMessageRequest(
            ReceiverId: "test_user_2",
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
        Assert.Equal("test_user_2", conversations[0].OtherParticipant.UserId);
    }

    [Fact]
    public async Task GetMessages_ValidConversation_ReturnsMessages()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // Send a message to create conversation
        var sendRequest = new SendMessageRequest(
            ReceiverId: "test_user_2",
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
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // Send multiple messages
        var conversationId = "";
        for (int i = 1; i <= 5; i++)
        {
            var sendRequest = new SendMessageRequest(
                ReceiverId: "test_user_2",
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
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // Send a message first
        var sendRequest = new SendMessageRequest(
            ReceiverId: "test_user_2",
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
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // Send a message first
        var sendRequest = new SendMessageRequest(
            ReceiverId: "test_user_2",
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
        // Note: This endpoint returns empty OK response, not a ChatMessage - don't deserialize
    }

    [Fact]
    public async Task DeleteMessage_ValidRequest_ReturnsOk()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // Send a message first
        var sendRequest = new SendMessageRequest(
            ReceiverId: "test_user_2",
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
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // Send to user2
        await _client.PostAsJsonAsync("/api/chat/messages", new SendMessageRequest(
            ReceiverId: "test_user_2",
            Content: "Hello user2",
            MessageType: "text",
            Metadata: null
        ));

        // Send to user3
        await _client.PostAsJsonAsync("/api/chat/messages", new SendMessageRequest(
            ReceiverId: "test_user_3",
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
        Assert.Contains("test_user_2", userIds);
        Assert.Contains("test_user_3", userIds);
    }

    [Fact]
    public async Task GetMessages_NonExistentConversation_ReturnsEmptyList()
    {
        // Arrange
        await SetupTestUsersAsync();

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-session-token");

        // Act
        var response = await _client.GetAsync("/api/chat/conversations/nonexistent_conv/messages");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = await response.Content.ReadFromJsonAsync<List<ChatMessage>>();
        Assert.NotNull(messages);
        Assert.Empty(messages);
    }
}
