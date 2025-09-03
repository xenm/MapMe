using System.Net;
using System.Security.Claims;
using MapMe.Logging;
using MapMe.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MapMe.Tests.Unit;

/// <summary>
/// Simplified unit tests for SecureLoggerDecorator focusing on core security functionality
/// </summary>
public class SecureLoggerDecoratorSimpleTests
{
    private readonly SecureLoggerDecorator<string> _decorator;
    private readonly Mock<ILogger<string>> _mockLogger;
    private readonly Mock<ISecureLoggingService> _mockSecureLoggingService;

    public SecureLoggerDecoratorSimpleTests()
    {
        _mockSecureLoggingService = new Mock<ISecureLoggingService>();
        _mockLogger = new Mock<ILogger<string>>();
        _decorator = new SecureLoggerDecorator<string>(_mockLogger.Object, _mockSecureLoggingService.Object);
    }

    #region IsEnabled Tests

    [Theory]
    [InlineData(LogLevel.Trace, true)]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Information, true)]
    [InlineData(LogLevel.Warning, true)]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Critical, true)]
    public void IsEnabled_DelegatesToUnderlyingLogger(LogLevel logLevel, bool expectedResult)
    {
        // Arrange
        _mockLogger.Setup(x => x.IsEnabled(logLevel)).Returns(expectedResult);

        // Act
        var result = _decorator.IsEnabled(logLevel);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockLogger.Verify(x => x.IsEnabled(logLevel), Times.Once);
    }

    #endregion

    #region BeginScope Tests

    [Fact]
    public void BeginScope_DelegatesToUnderlyingLogger()
    {
        // Arrange
        var state = new { TestProperty = "TestValue" };
        var mockScope = new Mock<IDisposable>();
        _mockLogger.Setup(x => x.BeginScope(state)).Returns(mockScope.Object);

        // Act
        var scope = _decorator.BeginScope(state);

        // Assert
        Assert.NotNull(scope);
        Assert.Equal(mockScope.Object, scope);
        _mockLogger.Verify(x => x.BeginScope(state), Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecureLoggerDecorator<string>(null!, _mockSecureLoggingService.Object));
    }

    [Fact]
    public void Constructor_WithNullSecureLoggingService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecureLoggerDecorator<string>(_mockLogger.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var decorator = new SecureLoggerDecorator<string>(_mockLogger.Object, _mockSecureLoggingService.Object);

        // Assert
        Assert.NotNull(decorator);
    }

    #endregion

    #region Log Method Tests

    [Fact]
    public void Log_CallsSecureLoggingServiceAndUnderlyingLogger()
    {
        // Arrange
        var secureContext = new { UserId = Guid.NewGuid(), EventType = "TestEvent" };
        _mockSecureLoggingService.Setup(x => x.CreateSecureLogContext(It.IsAny<object>()))
            .Returns(secureContext);
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var logLevel = LogLevel.Information;
        var eventId = new EventId(1, "TestEvent");
        var state = "Test message";
        Exception? exception = null;

        // Act
        _decorator.Log(logLevel, eventId, state, exception, (s, e) => s);

        // Assert
        _mockSecureLoggingService.Verify(x => x.CreateSecureLogContext(It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.Log(
            logLevel,
            eventId,
            It.IsAny<It.IsAnyType>(),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Log_WithSecureContext_CallsUnderlyingLoggerWithEnhancedState()
    {
        // Arrange
        var secureContext = new { UserId = Guid.NewGuid(), EventType = "TestEvent", LogLevel = "Information" };
        _mockSecureLoggingService.Setup(x => x.CreateSecureLogContext(It.IsAny<object>()))
            .Returns(secureContext);
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var logLevel = LogLevel.Information;
        var eventId = new EventId(1, "TestEvent");
        var state = "Test message";

        // Act
        _decorator.Log(logLevel, eventId, state, null, (s, e) => s);

        // Assert
        _mockSecureLoggingService.Verify(x => x.CreateSecureLogContext(It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.Log(
            logLevel,
            eventId,
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Log_WithSecureLoggingService_DoesNotThrow()
    {
        // Arrange
        var secureContext = new { UserId = Guid.NewGuid(), EventType = "SecurityEvent", LogLevel = "Warning" };
        _mockSecureLoggingService.Setup(x => x.CreateSecureLogContext(It.IsAny<object>()))
            .Returns(secureContext);
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var logLevel = LogLevel.Warning;
        var eventId = new EventId(1, "SecurityEvent");
        var state = "Security test";

        // Act & Assert - Should not throw
        _decorator.Log(logLevel, eventId, state, null, (s, e) => s);

        // Verify the secure logging service and underlying logger were called
        _mockSecureLoggingService.Verify(x => x.CreateSecureLogContext(It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.Log(
            logLevel,
            eventId,
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Log_WithUserContext_DoesNotThrow()
    {
        // Arrange
        var secureContext = new { UserId = Guid.NewGuid(), EventType = "JwtEvent", LogLevel = "Information" };
        _mockSecureLoggingService.Setup(x => x.CreateSecureLogContext(It.IsAny<object>()))
            .Returns(secureContext);
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var logLevel = LogLevel.Information;
        var eventId = new EventId(1, "JwtEvent");
        var state = "JWT test";

        // Act & Assert - Should not throw
        _decorator.Log(logLevel, eventId, state, null, (s, e) => s);

        // Verify the secure logging service and underlying logger were called
        _mockSecureLoggingService.Verify(x => x.CreateSecureLogContext(It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.Log(
            logLevel,
            eventId,
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void Log_WithException_PreservesException()
    {
        // Arrange
        var secureContext = new
            { UserId = Guid.NewGuid(), EventType = "ErrorEvent", LogLevel = "Error", HasException = true };
        _mockSecureLoggingService.Setup(x => x.CreateSecureLogContext(It.IsAny<object>()))
            .Returns(secureContext);
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var logLevel = LogLevel.Error;
        var eventId = new EventId(1, "ErrorEvent");
        var state = "Error occurred";
        var exception = new InvalidOperationException("Test exception");

        // Act
        _decorator.Log(logLevel, eventId, state, exception, (s, e) => $"{s}: {e?.Message}");

        // Assert
        _mockSecureLoggingService.Verify(x => x.CreateSecureLogContext(It.IsAny<object>()), Times.Once);
        _mockLogger.Verify(x => x.Log(
            logLevel,
            eventId,
            It.IsAny<It.IsAnyType>(),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateBasicHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/test";
        context.Request.Headers["User-Agent"] = "Test Browser";
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");
        return context;
    }

    private static DefaultHttpContext CreateMaliciousHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST\nmalicious";
        context.Request.Path = "/api\r/test<script>alert('xss')</script>";
        context.Request.Headers["User-Agent"] = "Browser\nwith\nnewlines<img src=x onerror=alert(1)>";
        context.Request.Headers["Authorization"] = "Bearer malicious\ntoken\r<script>";
        return context;
    }

    private static DefaultHttpContext CreateHttpContextWithJwtClaims()
    {
        var context = CreateBasicHttpContext();

        var claims = new List<Claim>
        {
            new("sub", "user123"),
            new("unique_name", "testuser"),
            new("email", "test@example.com"),
            new("jti", "token-id-123")
        };

        var identity = new ClaimsIdentity(claims, "jwt");
        context.User = new ClaimsPrincipal(identity);

        return context;
    }

    #endregion
}