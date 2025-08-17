// Using fully qualified name for Blazor.Bootstrap to avoid namespace conflicts
using MapMe.Client.Pages;
using MapMe.Components;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using MapMe.Repositories;
using MapMe.DTOs;
using MapMe.Models;
using MapMe.Utils;
using Microsoft.Azure.Cosmos;
using System.Net.Http;
using MapMe.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add services to the container.
builder.Services.AddBlazorBootstrap();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    })
    .AddInteractiveWebAssemblyComponents();

// Add HttpContextAccessor for building request-based HttpClient base address
builder.Services.AddHttpContextAccessor();

// Register HttpClient for SSR/prerender so components that inject HttpClient work server-side
builder.Services.AddScoped(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var req = accessor.HttpContext?.Request;
    // Fallback to configured URLs if no current request (e.g., background ops)
    var baseUri = req is not null
        ? $"{req.Scheme}://{req.Host}"
        : (Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(';', StringSplitOptions.RemoveEmptyEntries)[0]
            ?? "https://localhost:8008");
    return new HttpClient { BaseAddress = new Uri(baseUri) };
});

// Data access: prefer Cosmos if configured; else, in-memory fallback
var cosmosEndpoint = builder.Configuration["Cosmos:Endpoint"];
var cosmosKey = builder.Configuration["Cosmos:Key"];
var cosmosDatabase = builder.Configuration["Cosmos:Database"] ?? "mapme";
var useCosmos = !string.IsNullOrWhiteSpace(cosmosEndpoint) && !string.IsNullOrWhiteSpace(cosmosKey);

if (useCosmos)
{
    builder.Services.AddSingleton(sp =>
    {
        // Allow self-signed cert for local emulator endpoints
        var isLocal = cosmosEndpoint!.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                    || cosmosEndpoint.Contains("127.0.0.1");
        var options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway
        };
        if (isLocal)
        {
            options.HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }
        return new CosmosClient(cosmosEndpoint!, cosmosKey!, options);
    });
    builder.Services.AddSingleton(new CosmosContextOptions(cosmosDatabase));
    builder.Services.AddSingleton<IUserProfileRepository, CosmosUserProfileRepository>();
    builder.Services.AddSingleton<IDateMarkByUserRepository, CosmosDateMarkByUserRepository>();
    // TODO: Add Cosmos implementations for chat repositories when needed
    builder.Services.AddSingleton<IChatMessageRepository, InMemoryChatMessageRepository>();
    builder.Services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
}
else
{
    // Temporary in-memory repositories
    builder.Services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();
    builder.Services.AddSingleton<IDateMarkByUserRepository, InMemoryDateMarkByUserRepository>();
    builder.Services.AddSingleton<IChatMessageRepository, InMemoryChatMessageRepository>();
    builder.Services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
}

// Register client-side services
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<ChatService>();

var app = builder.Build();

// Ensure default user profile exists for development
await EnsureDefaultUserProfileAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForErrors: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MapMe.Client._Imports).Assembly);

// Minimal API to provide client with Google Maps API key (prefer config/user-secrets over env var)
app.MapGet("/config/maps", (HttpContext http) =>
{
    // Prefer configuration first (includes appsettings and User Secrets in Development)
    var apiKey = app.Configuration["GoogleMaps:ApiKey"];
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        // Fallback to environment variable
        apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
    }

    return Results.Ok(new { ApiKey = apiKey });
});

// Profiles API
app.MapPost("/api/profiles", async (CreateProfileRequest req, IUserProfileRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.Id) || string.IsNullOrWhiteSpace(req.UserId) || string.IsNullOrWhiteSpace(req.DisplayName))
        return Results.BadRequest("Id, UserId and DisplayName are required");
    var now = DateTimeOffset.UtcNow;
    var profile = req.ToProfile(now);
    await repo.UpsertAsync(profile);
    return Results.Created($"/api/profiles/{profile.Id}", profile);
});

app.MapGet("/api/profiles/{id}", async (string id, IUserProfileRepository repo) =>
{
    var profile = await repo.GetByIdAsync(id);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

// Add endpoint for current user (for JavaScript compatibility)
app.MapGet("/api/users/current_user", async (HttpContext context, IUserProfileRepository repo) =>
{
    // Get user ID from header (simulated authentication)
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "current_user";
    var profile = await repo.GetByUserIdAsync(userId);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

app.MapGet("/api/users/{userId}", async (string userId, IUserProfileRepository repo) =>
{
    var profile = await repo.GetByUserIdAsync(userId);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
});

// DateMarks API
app.MapPost("/api/datemarks", async (UpsertDateMarkRequest req, IDateMarkByUserRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.Id) || string.IsNullOrWhiteSpace(req.UserId))
        return Results.BadRequest("Id and UserId are required");
    var now = DateTimeOffset.UtcNow;
    var mark = req.ToDateMark(now);
    await repo.UpsertAsync(mark);
    return Results.Created($"/api/datemarks/{mark.Id}", mark);
});

app.MapGet("/api/users/{userId}/datemarks", async (
    string userId,
    DateOnly? from,
    DateOnly? to,
    string[]? categories,
    string[]? tags,
    string[]? qualities,
    IDateMarkByUserRepository repo,
    CancellationToken ct) =>
{
    var cats = categories is { Length: > 0 } ? Normalization.ToNorm(categories!) : Array.Empty<string>();
    var tgs = tags is { Length: > 0 } ? Normalization.ToNorm(tags!) : Array.Empty<string>();
    var qls = qualities is { Length: > 0 } ? Normalization.ToNorm(qualities!) : Array.Empty<string>();
    var list = new List<DateMark>();
    await foreach (var dm in repo.GetByUserAsync(userId, from, to, cats, tgs, qls, ct))
    {
        list.Add(dm);
    }
    return Results.Ok(list);
});

// Map viewport query (prototype: radius around lat/lng); later switch to bbox & geohash prefixes
app.MapGet("/api/map/datemarks", async (
    double lat,
    double lng,
    double radiusMeters,
    string[]? categories,
    string[]? tags,
    string[]? qualities,
    IDateMarkByUserRepository repo,
    CancellationToken ct) =>
{
    // For prototype, scan all in-memory marks; will be replaced by DateMarksGeo + prefixes
    var cats = categories is { Length: > 0 } ? Normalization.ToNorm(categories!) : Array.Empty<string>();
    var tgs = tags is { Length: > 0 } ? Normalization.ToNorm(tags!) : Array.Empty<string>();
    var qls = qualities is { Length: > 0 } ? Normalization.ToNorm(qualities!) : Array.Empty<string>();

    // In-memory store is per user; aggregate across users
    var results = new List<DateMark>();
    // Access internal repo data is not exposed; for prototype we iterate by known users via reflection is overkill.
    // Instead, require a userId for now or expand repository later. Here we just return empty to keep API shape stable.
    return Results.Ok(results);
});

// Chat API Endpoints
app.MapPost("/api/chat/messages", async (SendMessageRequest req, IChatMessageRepository messageRepo, IConversationRepository conversationRepo, IUserProfileRepository userRepo, HttpContext context) =>
{
    // For now, use a default current user ID - in production this would come from authentication
    var senderId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "current_user";
    
    if (string.IsNullOrWhiteSpace(req.ReceiverId) || string.IsNullOrWhiteSpace(req.Content))
        return Results.BadRequest("ReceiverId and Content are required");

    // Allow self-messaging for testing purposes
    if (req.ReceiverId == senderId)
    {
        // For self-messaging, we still need to ensure the user exists
        var selfUser = await userRepo.GetByUserIdAsync(senderId);
        if (selfUser == null)
            return Results.BadRequest("User not found");
    }
    else
    {
        // Verify receiver exists (for different users)
        var receiver = await userRepo.GetByUserIdAsync(req.ReceiverId);
        if (receiver == null)
            return Results.BadRequest("Receiver not found");
    }

    var now = DateTimeOffset.UtcNow;
    var message = req.ToChatMessage(senderId, now);

    // Save message
    await messageRepo.UpsertAsync(message);

    // Get or create conversation
    var conversation = await conversationRepo.GetOrCreateConversationAsync(senderId, req.ReceiverId);

    // Update conversation metadata
    await conversationRepo.UpdateConversationMetadataAsync(
        conversation.Id,
        message.Id,
        message.Content,
        senderId,
        now);

    // Update unread count for receiver
    var receiverUnreadCount = await messageRepo.GetUnreadCountAsync(conversation.Id, req.ReceiverId);
    await conversationRepo.UpdateUnreadCountAsync(conversation.Id, req.ReceiverId, receiverUnreadCount);

    return Results.Created($"/api/chat/messages/{message.Id}", message);
});

app.MapGet("/api/chat/conversations", async (IConversationRepository conversationRepo, IUserProfileRepository userRepo, HttpContext context) =>
{
    // For now, use a default current user ID - in production this would come from authentication
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "current_user";
    
    var conversations = new List<ConversationResponse>();
    
    await foreach (var conversation in conversationRepo.GetByUserAsync(userId))
    {
        var otherParticipantId = conversation.GetOtherParticipantId(userId);
        var otherParticipant = await userRepo.GetByUserIdAsync(otherParticipantId);
        
        if (otherParticipant == null) continue;

        var conversationResponse = new ConversationResponse(
            Id: conversation.Id,
            OtherParticipant: new MapMe.DTOs.UserSummary(
                UserId: otherParticipant.UserId,
                DisplayName: otherParticipant.DisplayName,
                AvatarUrl: otherParticipant.Photos.FirstOrDefault()?.Url
            ),
            LastMessage: conversation.LastMessageContent != null ? new MapMe.DTOs.MessageSummary(
                Id: conversation.LastMessageId!,
                Content: conversation.LastMessageContent,
                MessageType: "text", // Default for now
                SenderId: conversation.LastMessageSenderId!,
                CreatedAt: conversation.LastMessageAt!.Value,
                IsRead: conversation.GetUnreadCountForUser(userId) == 0
            ) : null,
            UnreadCount: conversation.GetUnreadCountForUser(userId),
            IsArchived: conversation.IsArchivedForUser(userId),
            CreatedAt: conversation.CreatedAt,
            UpdatedAt: conversation.UpdatedAt
        );
        
        conversations.Add(conversationResponse);
    }

    return Results.Ok(conversations.OrderByDescending(c => c.LastMessage?.CreatedAt ?? c.CreatedAt));
});

app.MapGet("/api/chat/conversations/{conversationId}/messages", async (
    string conversationId, 
    IChatMessageRepository messageRepo,
    IConversationRepository conversationRepo,
    HttpContext context,
    int skip = 0,
    int take = 50) =>
{
    // For now, use a default current user ID - in production this would come from authentication
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "current_user";
    
    // Verify user is participant in conversation
    var conversation = await conversationRepo.GetByIdAsync(conversationId);
    if (conversation == null || (conversation.Participant1Id != userId && conversation.Participant2Id != userId))
        return Results.Ok(new List<ChatMessage>());

    var messages = new List<ChatMessage>();
    await foreach (var message in messageRepo.GetByConversationAsync(conversationId, skip, take))
    {
        messages.Add(message);
    }

    return Results.Ok(messages);
});

app.MapPost("/api/chat/messages/read", async (MarkAsReadRequest req, IChatMessageRepository messageRepo, IConversationRepository conversationRepo, HttpContext context) =>
{
    // For now, use a default current user ID - in production this would come from authentication
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "current_user";
    
    // Verify user is participant in conversation
    var conversation = await conversationRepo.GetByIdAsync(req.ConversationId);
    if (conversation == null || (conversation.Participant1Id != userId && conversation.Participant2Id != userId))
        return Results.Forbid();

    // Mark messages as read
    await messageRepo.MarkAsReadAsync(req.ConversationId, userId);

    // Update conversation unread count
    await conversationRepo.UpdateUnreadCountAsync(req.ConversationId, userId, 0);

    return Results.Ok();
});

app.MapPost("/api/chat/conversations/archive", async (ArchiveConversationRequest req, IConversationRepository conversationRepo, HttpContext context) =>
{
    // For now, use a default current user ID - in production this would come from authentication
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "current_user";
    
    // Verify user is participant in conversation
    var conversation = await conversationRepo.GetByIdAsync(req.ConversationId);
    if (conversation == null || (conversation.Participant1Id != userId && conversation.Participant2Id != userId))
        return Results.Forbid();

    await conversationRepo.SetArchiveStatusAsync(req.ConversationId, userId, req.IsArchived);

    return Results.Ok();
});

app.MapDelete("/api/chat/messages/{messageId}", async (string messageId, IChatMessageRepository messageRepo, HttpContext context) =>
{
    // For now, use a default current user ID - in production this would come from authentication
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "current_user";
    
    var message = await messageRepo.GetByIdAsync(messageId);
    if (message == null || message.SenderId != userId)
        return Results.Forbid();

    await messageRepo.DeleteAsync(messageId);

    return Results.Ok();
});

app.MapGet("/api/chat/messages/new", async (IChatMessageRepository messageRepo, IConversationRepository conversationRepo, HttpContext context) =>
{
    // For now, use a default current user ID - in production this would come from authentication
    var userId = context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "current_user";
    
    // Get all conversations for the user and return recent messages
    var conversations = new List<Conversation>();
    await foreach (var conversation in conversationRepo.GetByUserAsync(userId))
    {
        conversations.Add(conversation);
    }

    var recentMessages = new List<ChatMessage>();
    foreach (var conversation in conversations.Take(10)) // Limit to recent conversations
    {
        await foreach (var message in messageRepo.GetByConversationAsync(conversation.Id, 0, 5))
        {
            recentMessages.Add(message);
        }
    }

    return Results.Ok(recentMessages.OrderByDescending(m => m.CreatedAt).Take(20));
});

app.MapGet("/messages/new", async (HttpContext context) =>
{
    var to = context.Request.Query["to"].FirstOrDefault() ?? "current_user";
    // Redirect to chat page with the target user
    return Results.Redirect($"/chat?to={Uri.EscapeDataString(to)}");
});

app.Run();

/// <summary>
/// Ensures a default user profile exists for development purposes
/// </summary>
static async Task EnsureDefaultUserProfileAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userRepo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
    
    try
    {
        // Check if current_user profile exists
        var existingProfile = await userRepo.GetByUserIdAsync("current_user");
        if (existingProfile == null)
        {
            // Create default user profile using correct model structure
            var defaultProfile = new UserProfile(
                Id: Guid.NewGuid().ToString(),
                UserId: "current_user",
                DisplayName: "Current User",
                Bio: "Default user profile for development",
                Photos: new List<UserPhoto>
                {
                    new UserPhoto(
                        Url: "https://via.placeholder.com/400x400/007bff/ffffff?text=User",
                        IsPrimary: true
                    )
                }.AsReadOnly(),
                Preferences: new UserPreferences(
                    Categories: new List<string> { "Technology", "Travel", "Food" }.AsReadOnly()
                ),
                Visibility: "public",
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: DateTimeOffset.UtcNow
            );
            
            await userRepo.UpsertAsync(defaultProfile);
        }
    }
    catch (Exception ex)
    {
        // Log error but don't fail startup
        Console.WriteLine($"Warning: Could not ensure default user profile: {ex.Message}");
    }
}

// Expose Program for WebApplicationFactory in tests
public partial class Program { }