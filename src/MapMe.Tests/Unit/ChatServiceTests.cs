using MapMe.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace MapMe.Tests.Unit;

/// <summary>
/// Unit tests for ChatService - basic functionality tests
/// </summary>
public class ChatServiceTests
{
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly Mock<IJSRuntime> _mockJSRuntime;
    private readonly Mock<NavigationManager> _mockNavigationManager;

    public ChatServiceTests()
    {
        _mockJSRuntime = new Mock<IJSRuntime>();
        _mockHttpClient = new Mock<HttpClient>();
        _mockNavigationManager = new Mock<NavigationManager>();
    }

    [Fact]
    public void ChatService_Constructor_InitializesCorrectly()
    {
        // Act
        var service = new ChatService(_mockJSRuntime.Object, _mockHttpClient.Object, _mockNavigationManager.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void ChatService_HasCorrectPublicMethods()
    {
        // Arrange & Act
        var serviceType = typeof(ChatService);

        // Assert - Verify key public methods exist
        Assert.NotNull(serviceType.GetMethod("SendMessageAsync"));
        Assert.NotNull(serviceType.GetMethod("GetConversationsAsync"));
        Assert.NotNull(serviceType.GetMethod("GetMessagesAsync"));
        Assert.NotNull(serviceType.GetMethod("MarkAsReadAsync"));
        Assert.NotNull(serviceType.GetMethod("ArchiveConversationAsync"));
        Assert.NotNull(serviceType.GetMethod("DeleteMessageAsync"));
    }

    [Fact]
    public void ChatService_PublicMethods_ReturnCorrectTypes()
    {
        // Arrange
        var serviceType = typeof(ChatService);

        // Act & Assert - Verify method return types
        var sendMessageMethod = serviceType.GetMethod("SendMessageAsync");
        Assert.NotNull(sendMessageMethod);
        Assert.True(sendMessageMethod.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), sendMessageMethod.ReturnType.GetGenericTypeDefinition());

        var getConversationsMethod = serviceType.GetMethod("GetConversationsAsync");
        Assert.NotNull(getConversationsMethod);
        Assert.True(getConversationsMethod.ReturnType.IsGenericType);

        var getMessagesMethod = serviceType.GetMethod("GetMessagesAsync");
        Assert.NotNull(getMessagesMethod);
        Assert.True(getMessagesMethod.ReturnType.IsGenericType);

        var markAsReadMethod = serviceType.GetMethod("MarkAsReadAsync");
        Assert.NotNull(markAsReadMethod);
        Assert.True(markAsReadMethod.ReturnType.IsGenericType);

        var archiveMethod = serviceType.GetMethod("ArchiveConversationAsync");
        Assert.NotNull(archiveMethod);
        Assert.True(archiveMethod.ReturnType.IsGenericType);

        var deleteMethod = serviceType.GetMethod("DeleteMessageAsync");
        Assert.NotNull(deleteMethod);
        Assert.True(deleteMethod.ReturnType.IsGenericType);
    }
}