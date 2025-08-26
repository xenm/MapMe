using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using MapMe.Client.Models;

namespace MapMe.Client.Services;

/// <summary>
/// Service for managing chat functionality with real-time messaging capabilities
/// </summary>
public class ChatService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private const string ConversationsStorageKey = "conversations";
    private const string MessagesStorageKey = "messages";

    public ChatService(IJSRuntime jsRuntime, HttpClient httpClient, NavigationManager navigationManager)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
        _navigationManager = navigationManager;
    }

    /// <summary>
    /// Send a message to another user
    /// </summary>
    public async Task<ChatMessage?> SendMessageAsync(string receiverId, string content, string messageType = "text", MessageMetadata? metadata = null)
    {
        try
        {
            var request = new
            {
                receiverId = receiverId,
                content = content,
                messageType = messageType,
                metadata = metadata
            };

            // Set current user header for API authentication
            _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
            _httpClient.DefaultRequestHeaders.Add("X-User-Id", "current_user");

            var response = await _httpClient.PostAsJsonAsync("/api/chat/messages", request);
            
            if (response.IsSuccessStatusCode)
            {
                var messageJson = await response.Content.ReadAsStringAsync();
                var message = JsonSerializer.Deserialize<ChatMessage>(messageJson);
                
                // Store message locally for offline access
                if (message != null)
                {
                    await StoreMessageLocallyAsync(message);
                }
                
                return message;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    public async Task<List<ConversationSummary>> GetConversationsAsync()
    {
        try
        {
            // Set current user header for API authentication
            _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
            _httpClient.DefaultRequestHeaders.Add("X-User-Id", "current_user");

            var response = await _httpClient.GetAsync("/api/chat/conversations");
            
            if (response.IsSuccessStatusCode)
            {
                var conversationsJson = await response.Content.ReadAsStringAsync();
                var conversations = JsonSerializer.Deserialize<List<ConversationSummary>>(conversationsJson) ?? new();
                
                // Store conversations locally for offline access
                await StoreConversationsLocallyAsync(conversations);
                
                return conversations;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading conversations: {ex.Message}");
        }

        // Fallback to local storage
        return await GetConversationsFromStorageAsync();
    }

    /// <summary>
    /// Get messages for a specific conversation
    /// </summary>
    public async Task<List<ChatMessage>> GetMessagesAsync(string conversationId, int skip = 0, int take = 50)
    {
        try
        {
            // Set current user header for API authentication
            _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
            _httpClient.DefaultRequestHeaders.Add("X-User-Id", "current_user");

            var response = await _httpClient.GetAsync($"/api/chat/conversations/{conversationId}/messages?skip={skip}&take={take}");
            
            if (response.IsSuccessStatusCode)
            {
                var messagesJson = await response.Content.ReadAsStringAsync();
                var messages = JsonSerializer.Deserialize<List<ChatMessage>>(messagesJson) ?? new();
                
                // Store messages locally for offline access
                foreach (var message in messages)
                {
                    await StoreMessageLocallyAsync(message);
                }
                
                return messages;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading messages: {ex.Message}");
        }

        // Fallback to local storage
        return await GetMessagesFromStorageAsync(conversationId);
    }

    /// <summary>
    /// Mark messages as read in a conversation
    /// </summary>
    public async Task<bool> MarkAsReadAsync(string conversationId)
    {
        try
        {
            var request = new { conversationId = conversationId };

            // Set current user header for API authentication
            _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
            _httpClient.DefaultRequestHeaders.Add("X-User-Id", "current_user");

            var response = await _httpClient.PostAsJsonAsync("/api/chat/messages/read", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking messages as read: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Archive or unarchive a conversation
    /// </summary>
    public async Task<bool> ArchiveConversationAsync(string conversationId, bool isArchived)
    {
        try
        {
            var request = new { conversationId = conversationId, isArchived = isArchived };

            // Set current user header for API authentication
            _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
            _httpClient.DefaultRequestHeaders.Add("X-User-Id", "current_user");

            var response = await _httpClient.PostAsJsonAsync("/api/chat/conversations/archive", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error archiving conversation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    public async Task<bool> DeleteMessageAsync(string messageId)
    {
        try
        {
            // Set current user header for API authentication
            _httpClient.DefaultRequestHeaders.Remove("X-User-Id");
            _httpClient.DefaultRequestHeaders.Add("X-User-Id", "current_user");

            var response = await _httpClient.DeleteAsync($"/api/chat/messages/{messageId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting message: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Start a new conversation with a user
    /// </summary>
    public Task<string?> StartConversationAsync(string otherUserId)
    {
        // Create conversation ID using the same logic as backend
        var currentUserId = "current_user";
        var orderedIds = new[] { currentUserId, otherUserId }.OrderBy(x => x).ToArray();
        return Task.FromResult<string?>($"conv_{orderedIds[0]}_{orderedIds[1]}");
    }

    /// <summary>
    /// Start a chat with a user (creates conversation and navigates to it)
    /// </summary>
    public async Task StartChatAsync(string otherUserId)
    {
        var conversationId = await StartConversationAsync(otherUserId);
        if (!string.IsNullOrEmpty(conversationId))
        {
            // Navigate to the chat page with the conversation
            _navigationManager.NavigateTo($"/chat/{conversationId}");
        }
    }

    /// <summary>
    /// Start a chat with a user about a specific Date Mark (creates conversation, sends initial message, and navigates to it)
    /// </summary>
    public async Task StartChatAsync(string otherUserId, DateMark dateMark)
    {
        var conversationId = await StartConversationAsync(otherUserId);
        if (!string.IsNullOrEmpty(conversationId))
        {
            // Send an initial message about the Date Mark
            var initialMessage = $"Hi! I saw your Date Mark at {dateMark.Name ?? "this place"} and wanted to chat about it.";
            
            try
            {
                await SendMessageAsync(otherUserId, initialMessage, "datemark", new MessageMetadata
                {
                    DateMarkId = dateMark.Id,
                    DateMarkName = dateMark.Name,
                    LocationName = dateMark.Name,
                    Latitude = dateMark.Latitude,
                    Longitude = dateMark.Longitude
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending initial DateMark message: {ex.Message}");
                // Continue to navigate even if message sending fails
            }
            
            // Navigate to the chat page with the conversation
            _navigationManager.NavigateTo($"/chat/{conversationId}");
        }
    }

    /// <summary>
    /// Get total unread message count across all conversations
    /// </summary>
    public async Task<int> GetTotalUnreadCountAsync()
    {
        try
        {
            var conversations = await GetConversationsAsync();
            return conversations.Sum(c => c.UnreadCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting unread count: {ex.Message}");
            return 0;
        }
    }

    #region Local Storage Methods

    private async Task StoreMessageLocallyAsync(ChatMessage message)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", MessagesStorageKey);
            var messages = string.IsNullOrWhiteSpace(json) 
                ? new List<ChatMessage>() 
                : JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new();

            // Remove existing message with same ID and add updated one
            messages.RemoveAll(m => m.Id == message.Id);
            messages.Add(message);

            // Keep only recent messages (last 1000 per conversation)
            var messagesByConversation = messages.GroupBy(m => m.ConversationId);
            var recentMessages = new List<ChatMessage>();
            
            foreach (var group in messagesByConversation)
            {
                recentMessages.AddRange(group.OrderByDescending(m => m.CreatedAt).Take(1000));
            }

            var updatedJson = JsonSerializer.Serialize(recentMessages);
            await _jsRuntime.InvokeVoidAsync("MapMe.storage.save", MessagesStorageKey, updatedJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing message locally: {ex.Message}");
        }
    }

    private async Task StoreConversationsLocallyAsync(List<ConversationSummary> conversations)
    {
        try
        {
            var json = JsonSerializer.Serialize(conversations);
            await _jsRuntime.InvokeVoidAsync("MapMe.storage.save", ConversationsStorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing conversations locally: {ex.Message}");
        }
    }

    private async Task<List<ConversationSummary>> GetConversationsFromStorageAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", ConversationsStorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                return JsonSerializer.Deserialize<List<ConversationSummary>>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading conversations from storage: {ex.Message}");
        }

        return new List<ConversationSummary>();
    }

    private async Task<List<ChatMessage>> GetMessagesFromStorageAsync(string conversationId)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("MapMe.storage.load", MessagesStorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var allMessages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new();
                return allMessages
                    .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading messages from storage: {ex.Message}");
        }

        return new List<ChatMessage>();
    }

    #endregion
}

/// <summary>
/// Summary of a conversation for the conversations list
/// </summary>
public class ConversationSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("otherParticipant")]
    public UserSummary OtherParticipant { get; set; } = new();

    [JsonPropertyName("lastMessage")]
    public MessageSummary? LastMessage { get; set; }

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Summary of a user for conversation lists
/// </summary>
public class UserSummary
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Summary of a message for conversation lists
/// </summary>
public class MessageSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "text";

    [JsonPropertyName("senderId")]
    public string SenderId { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }
}
