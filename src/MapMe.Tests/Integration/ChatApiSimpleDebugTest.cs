using System.Net;
using System.Net.Http.Json;
using MapMe.DTOs;
using MapMe.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace MapMe.Tests.Integration;

/// <summary>
/// Simple debug test to understand the repository state issue
/// </summary>
public class ChatApiSimpleDebugTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public ChatApiSimpleDebugTest(WebApplicationFactory<Program> factory, ITestOutputHelper output)
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
        _output = output;
    }

    [Fact]
    public async Task Debug_FullFlow_StepByStep()
    {
        _output.WriteLine("=== Starting Full Flow Debug ===");

        // Step 1: Create user profiles
        _output.WriteLine("Step 1: Creating user profiles...");

        var user1ProfileRequest = new CreateProfileRequest(
            Id: Guid.NewGuid().ToString(),
            UserId: "user1",
            DisplayName: "Test User 1",
            Bio: "Test bio",
            Photos: null,
            PreferredCategories: null,
            Visibility: "public"
        );

        var user2ProfileRequest = new CreateProfileRequest(
            Id: Guid.NewGuid().ToString(),
            UserId: "user2",
            DisplayName: "Test User 2",
            Bio: "Test bio",
            Photos: null,
            PreferredCategories: null,
            Visibility: "public"
        );

        var profile1Response = await _client.PostAsJsonAsync("/api/profiles", user1ProfileRequest);
        _output.WriteLine($"User1 profile creation: {profile1Response.StatusCode}");
        var profile1Content = await profile1Response.Content.ReadAsStringAsync();
        _output.WriteLine($"User1 profile response: {profile1Content}");

        var profile2Response = await _client.PostAsJsonAsync("/api/profiles", user2ProfileRequest);
        _output.WriteLine($"User2 profile creation: {profile2Response.StatusCode}");
        var profile2Content = await profile2Response.Content.ReadAsStringAsync();
        _output.WriteLine($"User2 profile response: {profile2Content}");

        // Step 2: Verify profiles can be retrieved
        _output.WriteLine("\nStep 2: Verifying profiles can be retrieved...");

        var getProfile1Response = await _client.GetAsync($"/api/profiles/{user1ProfileRequest.Id}");
        _output.WriteLine($"Get User1 profile: {getProfile1Response.StatusCode}");

        var getProfile2Response = await _client.GetAsync($"/api/profiles/{user2ProfileRequest.Id}");
        _output.WriteLine($"Get User2 profile: {getProfile2Response.StatusCode}");

        // Step 3: Try to send a message
        _output.WriteLine("\nStep 3: Attempting to send message...");

        var sendRequest = new SendMessageRequest(
            ReceiverId: "user2",
            Content: "Hello there!",
            MessageType: "text",
            Metadata: null
        );

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-User-Id", "user1");

        var sendResponse = await _client.PostAsJsonAsync("/api/chat/messages", sendRequest);
        _output.WriteLine($"Send message status: {sendResponse.StatusCode}");
        var sendContent = await sendResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Send message response: {sendContent}");

        // Step 4: If message sending failed, let's debug the repository state
        if (sendResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            _output.WriteLine("\nStep 4: Debugging repository state...");

            // Try to manually check if we can find the users via a different approach
            // Let's try to create a test endpoint or use existing ones to verify user existence

            _output.WriteLine("Message sending failed. Let's check if the issue is with user lookup.");

            // The issue might be that the chat API is using a different repository instance
            // or the repository state is not being shared properly between requests
        }

        _output.WriteLine("=== Debug Complete ===");
    }
}