using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace MapMe.Tests.Integration;

/// <summary>
/// Debug tests to understand what the Chat API is actually returning
/// </summary>
public class ChatApiDebugTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public ChatApiDebugTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace repositories with in-memory implementations for testing
                services.AddScoped<IChatMessageRepository, InMemoryChatMessageRepository>();
                services.AddScoped<IConversationRepository, InMemoryConversationRepository>();
                services.AddScoped<IUserProfileRepository, InMemoryUserProfileRepository>();
            });
        });
        _client = _factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Debug_SendMessage_CheckResponse()
    {
        // First, let's create a user profile
        var profileRequest = new CreateProfileRequest(
            Id: Guid.NewGuid().ToString(),
            UserId: "user2",
            DisplayName: "Test User 2",
            Bio: "Test bio",
            Photos: null,
            PreferredCategories: null,
            Visibility: "public"
        );

        var profileResponse = await _client.PostAsJsonAsync("/api/profiles", profileRequest);
        _output.WriteLine($"Profile creation status: {profileResponse.StatusCode}");
        var profileContent = await profileResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Profile creation response: {profileContent}");

        // Now try to send a message
        var request = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Hello there!",
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        var response = await _client.PostAsJsonAsync("/api/chat/messages", request);
        _output.WriteLine($"Send message status: {response.StatusCode}");
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Send message response: {content}");

        // Let's also check if we can create the sender profile
        var senderProfileRequest = new CreateProfileRequest(
            Id: Guid.NewGuid().ToString(),
            UserId: "user1",
            DisplayName: "Test User 1",
            Bio: "Test bio",
            Photos: null,
            PreferredCategories: null,
            Visibility: "public"
        );

        var senderResponse = await _client.PostAsJsonAsync("/api/profiles", senderProfileRequest);
        _output.WriteLine($"Sender profile creation status: {senderResponse.StatusCode}");
        var senderContent = await senderResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Sender profile creation response: {senderContent}");

        // Try sending message again
        var response2 = await _client.PostAsJsonAsync("/api/chat/messages", request);
        _output.WriteLine($"Send message status (after sender profile): {response2.StatusCode}");
        
        var content2 = await response2.Content.ReadAsStringAsync();
        _output.WriteLine($"Send message response (after sender profile): {content2}");
    }

    [Fact]
    public async Task Debug_GetConversations_CheckResponse()
    {
        // Setup users first
        var users = new[]
        {
            new { userId = "user1", displayName = "Test User 1" },
            new { userId = "user2", displayName = "Test User 2" }
        };

        foreach (var user in users)
        {
            var profileRequest = new CreateProfileRequest(
                Id: Guid.NewGuid().ToString(),
                UserId: user.userId,
                DisplayName: user.displayName,
                Bio: "Test bio",
                Photos: null,
                PreferredCategories: null,
                Visibility: "public"
            );

            var profileResponse = await _client.PostAsJsonAsync("/api/profiles", profileRequest);
            _output.WriteLine($"Profile creation for {user.userId}: {profileResponse.StatusCode}");
        }

        // Send a message to create a conversation
        var request = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Hello!",
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        var sendResponse = await _client.PostAsJsonAsync("/api/chat/messages", request);
        _output.WriteLine($"Send message status: {sendResponse.StatusCode}");
        var sendContent = await sendResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Send message response: {sendContent}");

        // Now try to get conversations
        var response = await _client.GetAsync("/api/chat/conversations");
        _output.WriteLine($"Get conversations status: {response.StatusCode}");
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Get conversations response: {content}");
    }
}
