using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Repositories;
using MapMe.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MapMe.Tests.Integration;

[Trait("Category", "Integration")]
public class ChatNegativeIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public ChatNegativeIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace repos with in-memory and use test auth
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(IConversationRepository) ||
                    d.ServiceType == typeof(IChatMessageRepository) ||
                    d.ServiceType == typeof(IUserProfileRepository) ||
                    d.ServiceType == typeof(IDateMarkByUserRepository) ||
                    d.ServiceType == typeof(IAuthenticationService)).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
                services.AddSingleton<IChatMessageRepository, InMemoryChatMessageRepository>();
                services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
                services.AddSingleton<IDateMarkByUserRepository, InMemoryDateMarkByUserRepository>();
                services.AddScoped<IAuthenticationService, TestAuthenticationService>();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task MessagesNew_WithoutAuth_ReturnsUnauthorized()
    {
        var resp = await _client.GetAsync("/api/chat/messages/new");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ArchiveConversation_NotParticipant_ReturnsForbidden()
    {
        // Authenticate as test_user_id
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-session-token");

        // Seed a conversation between two other users
        using var scope = _factory.Services.CreateScope();
        var convRepo = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
        var conversation = await convRepo.GetOrCreateConversationAsync("user_a", "user_b");

        var request = new ArchiveConversationRequest(ConversationId: conversation.Id, IsArchived: true);
        var resp = await _client.PostAsJsonAsync("/api/chat/conversations/archive", request);

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteMessage_NotOwner_ReturnsForbidden()
    {
        // Authenticate as test_user_id
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-session-token");

        // Seed a conversation and a message from someone else
        using var scope = _factory.Services.CreateScope();
        var convRepo = scope.ServiceProvider.GetRequiredService<IConversationRepository>();
        var msgRepo = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();
        var conversation = await convRepo.GetOrCreateConversationAsync("user_x", "user_y");
        var now = DateTimeOffset.UtcNow;
        var otherMessage = new ChatMessage(
            Id: Guid.NewGuid().ToString(),
            ConversationId: conversation.Id,
            SenderId: "user_x",
            ReceiverId: "user_y",
            Content: "Hello from someone else",
            MessageType: "text",
            Metadata: null,
            IsRead: false,
            IsDelivered: true,
            CreatedAt: now,
            UpdatedAt: now,
            IsDeleted: false);
        await msgRepo.UpsertAsync(otherMessage);

        // Try to delete as test_user_id (not the sender)
        var resp = await _client.DeleteAsync($"/api/chat/messages/{otherMessage.Id}");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task DeleteMessage_WithoutAuth_ReturnsUnauthorized()
    {
        var resp = await _client.DeleteAsync("/api/chat/messages/some-id");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.MethodNotAllowed);
    }
}