using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MapMe.Authentication;
using MapMe.Models;
using MapMe.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MapMe.Tests.Unit;

/// <summary>
/// Comprehensive unit tests for JWT authentication handler
/// Tests middleware authentication, authorization, and security scenarios
/// </summary>
public class JwtAuthenticationHandlerCorrectedTests
{
    private readonly JwtAuthenticationHandler _handler;
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<ILogger<JwtAuthenticationHandler>> _mockLogger;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _mockOptions;
    private readonly Mock<ISecureLoggingService> _mockSecureLoggingService;
    private readonly Mock<UrlEncoder> _mockUrlEncoder;

    public JwtAuthenticationHandlerCorrectedTests()
    {
        _mockJwtService = new Mock<IJwtService>();
        _mockOptions = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<JwtAuthenticationHandler>>();
        _mockUrlEncoder = new Mock<UrlEncoder>();
        _mockSecureLoggingService = new Mock<ISecureLoggingService>();

        _httpContext = new DefaultHttpContext();

        // Setup mocks
        _mockOptions.Setup(o => o.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _handler = new JwtAuthenticationHandler(
            _mockOptions.Object,
            _mockLoggerFactory.Object,
            _mockUrlEncoder.Object,
            _mockJwtService.Object,
            _mockSecureLoggingService.Object);

        // Initialize handler with context
        _handler.InitializeAsync(
            new AuthenticationScheme("JWT", null, typeof(JwtAuthenticationHandler)),
            _httpContext).Wait();
    }

    #region Concurrency Tests

    [Fact]
    public async Task HandleAuthenticateAsync_ConcurrentRequests_ThreadSafe()
    {
        // Arrange
        var token = "valid.jwt.token";
        var userSession = CreateTestUserSession();

        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns(userSession);

        var tasks = new List<Task<AuthenticateResult>>();

        // Act - Process multiple requests concurrently
        for (int i = 0; i < 10; i++)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = $"Bearer {token}";

            var handler = new JwtAuthenticationHandler(
                _mockOptions.Object,
                _mockLoggerFactory.Object,
                _mockUrlEncoder.Object,
                _mockJwtService.Object,
                _mockSecureLoggingService.Object);

            await handler.InitializeAsync(
                new AuthenticationScheme("JWT", null, typeof(JwtAuthenticationHandler)),
                context);

            tasks.Add(handler.AuthenticateAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, result => Assert.True(result.Succeeded));
        _mockJwtService.Verify(s => s.ValidateToken(token), Times.Exactly(10));
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task HandleAuthenticateAsync_Performance_CompletesQuickly()
    {
        // Arrange
        var token = "valid.jwt.token";
        var userSession = CreateTestUserSession();

        _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns(userSession);

        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 100; i++)
        {
            await _handler.AuthenticateAsync();
        }

        // Assert
        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"100 authentications took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    #endregion

    #region Helper Methods

    private static UserSession CreateTestUserSession()
    {
        return new UserSession(
            UserId: "test-user-id-" + Guid.NewGuid(),
            Username: "testuser",
            Email: "test@example.com",
            SessionId: "session-" + Guid.NewGuid(),
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt: DateTimeOffset.UtcNow.AddHours(-1)
        );
    }

    #endregion

    #region Successful Authentication Tests

    [Fact]
    public async Task HandleAuthenticateAsync_ValidBearerToken_ReturnsSuccess()
    {
        // Arrange
        var token = "valid.jwt.token";
        var userSession = CreateTestUserSession();

        _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns(userSession);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal(userSession.UserId, result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(userSession.Username, result.Principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal(userSession.Email, result.Principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ValidTokenWithClaims_SetsCorrectClaims()
    {
        // Arrange
        var token = "valid.jwt.token";
        var userSession = new UserSession(
            UserId: "test-user-123",
            Username: "testuser",
            Email: "test@example.com",
            SessionId: "session-123",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt: DateTimeOffset.UtcNow.AddHours(-1)
        );

        _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns(userSession);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);

        var claims = result.Principal.Claims.ToList();
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == userSession.UserId);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == userSession.Username);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Email && c.Value == userSession.Email);
        Assert.Contains(claims, c => c.Type == "sessionId" && c.Value == userSession.SessionId);
    }

    #endregion

    #region Failed Authentication Tests

    [Fact]
    public async Task HandleAuthenticateAsync_NoAuthorizationHeader_ReturnsNoResult()
    {
        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.None);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_EmptyAuthorizationHeader_ReturnsNoResult()
    {
        // Arrange
        _httpContext.Request.Headers["Authorization"] = "";

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_NonBearerToken_ReturnsNoResult()
    {
        // Arrange
        _httpContext.Request.Headers["Authorization"] = "Basic dXNlcjpwYXNz";

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_BearerWithoutToken_ReturnsFailure()
    {
        // Arrange
        _httpContext.Request.Headers["Authorization"] = "Bearer";

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Failure != null);
        Assert.Contains("Invalid Authorization header format", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_InvalidToken_ReturnsFailure()
    {
        // Arrange - Use a clearly fake test token that won't trigger security scanners
        var invalidToken = "fake.test.token";
        _httpContext.Request.Headers["Authorization"] = $"Bearer {invalidToken}";
        _mockJwtService.Setup(s => s.ValidateToken(invalidToken))
            .Returns((UserSession?)null);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Failure != null);
        Assert.Contains("Invalid or expired token", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_JwtServiceThrowsException_ReturnsFailure()
    {
        // Arrange
        var token = "problematic.jwt.token";
        _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Throws(new Exception("JWT validation error"));

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Failure != null);
        Assert.Contains("Token validation failed", result.Failure.Message);
    }

    #endregion

    #region Authorization Header Parsing Tests

    [Fact]
    public async Task HandleAuthenticateAsync_BearerTokenWithExtraSpaces_ParsesCorrectly()
    {
        // Arrange
        var token = "valid.jwt.token";
        var userSession = CreateTestUserSession();

        _httpContext.Request.Headers["Authorization"] = $"Bearer   {token}   ";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns(userSession);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        _mockJwtService.Verify(s => s.ValidateToken(token), Times.Once);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_CaseInsensitiveBearer_ParsesCorrectly()
    {
        // Arrange
        var token = "valid.jwt.token";
        var userSession = CreateTestUserSession();

        _httpContext.Request.Headers["Authorization"] = $"bearer {token}";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns(userSession);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        _mockJwtService.Verify(s => s.ValidateToken(token), Times.Once);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_MultipleAuthorizationHeaders_ReturnsFailure()
    {
        // Arrange
        var token1 = "first.jwt.token";
        var token2 = "second.jwt.token";

        _httpContext.Request.Headers["Authorization"] = new[] { $"Bearer {token1}", $"Bearer {token2}" };

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Failure != null);
        Assert.Contains("Multiple Authorization headers not allowed", result.Failure.Message);
        // Verify no JWT validation was attempted for security
        _mockJwtService.Verify(s => s.ValidateToken(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task HandleAuthenticateAsync_VeryLongToken_HandlesGracefully()
    {
        // Arrange
        var longToken = new string('a', 10000); // 10KB token
        _httpContext.Request.Headers["Authorization"] = $"Bearer {longToken}";
        _mockJwtService.Setup(s => s.ValidateToken(longToken))
            .Returns((UserSession?)null);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Failure != null);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_TokenWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var token = "test-jwt-token-with-special-chars-123!@#$%";
        var userSession = CreateTestUserSession();

        _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns(userSession);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        _mockJwtService.Verify(s => s.ValidateToken(token), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task HandleAuthenticateAsync_NullUserSession_ReturnsFailure()
    {
        // Arrange
        var token = "valid.format.but.null.result";
        _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns((UserSession?)null);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Failure != null);
        Assert.Contains("Invalid or expired token", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_UserSessionWithNullValues_HandlesGracefully()
    {
        // Arrange
        var token = "valid.jwt.token";
        var userSession = new UserSession(
            UserId: "", // Empty user ID
            Username: "", // Empty username
            Email: "", // Empty email
            SessionId: "session-123",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            CreatedAt: DateTimeOffset.UtcNow.AddHours(-1)
        );

        _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockJwtService.Setup(s => s.ValidateToken(token))
            .Returns(userSession);

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded); // Should still succeed even with empty values
        Assert.NotNull(result.Principal);
    }

    #endregion
}