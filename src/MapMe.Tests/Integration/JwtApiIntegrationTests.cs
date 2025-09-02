using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MapMe.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MapMe.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for JWT authentication API endpoints
/// Tests complete authentication flow from registration to token validation
/// </summary>
public class JwtApiIntegrationCorrectedTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public JwtApiIntegrationCorrectedTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region Performance Tests

    [Fact]
    public async Task AuthenticationFlow_Performance_CompletesQuickly()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Complete authentication flow
        var token = await RegisterUserAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Authentication flow took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private async Task<string> RegisterUserAndGetToken()
    {
        var username = "testuser_" + Guid.NewGuid().ToString("N")[..8];
        var email = $"test_{Guid.NewGuid().ToString("N")[..8]}@example.com";
        var password = "Password123!";

        var request = new RegisterRequest(
            Username: username,
            Email: email,
            Password: password,
            ConfirmPassword: password,
            DisplayName: "Test User"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.NotNull(authResponse.Token);

        return authResponse.Token;
    }

    #endregion

    #region Registration Tests

    [Fact]
    public async Task Register_ValidUser_ReturnsSuccessWithJwtToken()
    {
        // Arrange
        var request = new RegisterRequest(
            Username: "testuser_" + Guid.NewGuid().ToString("N")[..8],
            Email: $"test_{Guid.NewGuid().ToString("N")[..8]}@example.com",
            Password: "Password123!",
            ConfirmPassword: "Password123!",
            DisplayName: "Test User"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.NotNull(authResponse.Token);
        Assert.NotEmpty(authResponse.Token);
        Assert.NotNull(authResponse.User);
        Assert.Equal(request.Username, authResponse.User.Username);
        Assert.Equal(request.Email, authResponse.User.Email);
        Assert.NotNull(authResponse.ExpiresAt);
        Assert.True(authResponse.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var username = "duplicate_" + Guid.NewGuid().ToString("N")[..8];
        var request1 = new RegisterRequest(
            Username: username,
            Email: $"test1_{Guid.NewGuid().ToString("N")[..8]}@example.com",
            Password: "Password123!",
            ConfirmPassword: "Password123!",
            DisplayName: "Test User 1"
        );
        var request2 = new RegisterRequest(
            Username: username,
            Email: $"test2_{Guid.NewGuid().ToString("N")[..8]}@example.com",
            Password: "Password123!",
            ConfirmPassword: "Password123!",
            DisplayName: "Test User 2"
        );

        // Act - Register first user
        var json1 = JsonSerializer.Serialize(request1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
        var response1 = await _client.PostAsync("/api/auth/register", content1);

        // Act - Try to register second user with same username
        var json2 = JsonSerializer.Serialize(request2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
        var response2 = await _client.PostAsync("/api/auth/register", content2);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest(
            Username: "testuser_" + Guid.NewGuid().ToString("N")[..8],
            Email: $"test_{Guid.NewGuid().ToString("N")[..8]}@example.com",
            Password: "short",
            ConfirmPassword: "short",
            DisplayName: "Test User"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordMismatch_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest(
            Username: "testuser_" + Guid.NewGuid().ToString("N")[..8],
            Email: $"test_{Guid.NewGuid().ToString("N")[..8]}@example.com",
            Password: "Password123!",
            ConfirmPassword: "DifferentPassword123!",
            DisplayName: "Test User"
        );

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccessWithJwtToken()
    {
        // Arrange - First register a user
        var username = "logintest_" + Guid.NewGuid().ToString("N")[..8];
        var email = $"logintest_{Guid.NewGuid().ToString("N")[..8]}@example.com";
        var password = "Password123!";

        var registerRequest = new RegisterRequest(
            Username: username,
            Email: email,
            Password: password,
            ConfirmPassword: password,
            DisplayName: "Login Test User"
        );

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        await _client.PostAsync("/api/auth/register", registerContent);

        // Act - Now login
        var loginRequest = new LoginRequest(
            Username: username,
            Password: password,
            RememberMe: false
        );

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/auth/login", loginContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.NotNull(authResponse.Token);
        Assert.NotEmpty(authResponse.Token);
        Assert.NotNull(authResponse.User);
        Assert.Equal(username, authResponse.User.Username);
        Assert.Equal(email, authResponse.User.Email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest(
            Username: "nonexistent_user",
            Password: "WrongPassword123!",
            RememberMe: false
        );

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest(
            Username: "",
            Password: "",
            RememberMe: false
        );

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsUserData()
    {
        // Arrange - Register and get token
        var token = await RegisterUserAndGetToken();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<AuthenticatedUser>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(user);
        Assert.NotNull(user.UserId);
        Assert.NotNull(user.Username);
        Assert.NotNull(user.Email);
    }

    [Fact]
    public async Task ValidateToken_NoToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidateToken_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidateToken_MalformedToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");

        // Act
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Google Login Tests

    [Fact]
    public async Task GoogleLogin_ValidRequest_ReturnsSuccessWithJwtToken()
    {
        // Arrange
        var googleRequest = new GoogleLoginRequest(
            GoogleToken: "mock-google-token",
            Email: $"google_{Guid.NewGuid().ToString("N")[..8]}@example.com",
            DisplayName: "Google Test User",
            GoogleId: "google-id-" + Guid.NewGuid().ToString("N")[..12]
        );

        var json = JsonSerializer.Serialize(googleRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/google-login", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(responseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.NotNull(authResponse.Token);
        Assert.NotEmpty(authResponse.Token);
        Assert.NotNull(authResponse.User);
        Assert.Equal(googleRequest.Email, authResponse.User.Email);
    }

    [Fact]
    public async Task GoogleLogin_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var googleRequest = new GoogleLoginRequest(
            GoogleToken: "",
            Email: "",
            DisplayName: "",
            GoogleId: ""
        );

        var json = JsonSerializer.Serialize(googleRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/google-login", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region End-to-End Authentication Flow Tests

    [Fact]
    public async Task CompleteAuthFlow_RegisterLoginValidate_AllSucceed()
    {
        // Arrange
        var username = "e2etest_" + Guid.NewGuid().ToString("N")[..8];
        var email = $"e2etest_{Guid.NewGuid().ToString("N")[..8]}@example.com";
        var password = "Password123!";

        // Step 1: Register
        var registerRequest = new RegisterRequest(
            Username: username,
            Email: email,
            Password: password,
            ConfirmPassword: password,
            DisplayName: "E2E Test User"
        );

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var registerResponseContent = await registerResponse.Content.ReadAsStringAsync();
        var registerAuthResponse = JsonSerializer.Deserialize<AuthenticationResponse>(registerResponseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(registerAuthResponse);
        Assert.True(registerAuthResponse.Success);
        Assert.NotNull(registerAuthResponse.Token);

        // Step 2: Login
        var loginRequest = new LoginRequest(
            Username: username,
            Password: password,
            RememberMe: false
        );

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
        var loginAuthResponse = JsonSerializer.Deserialize<AuthenticationResponse>(loginResponseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(loginAuthResponse);
        Assert.True(loginAuthResponse.Success);
        Assert.NotNull(loginAuthResponse.Token);

        // Step 3: Validate Token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginAuthResponse.Token);
        var validateResponse = await _client.GetAsync("/api/auth/validate-token");

        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);

        var validateResponseContent = await validateResponse.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<AuthenticatedUser>(validateResponseContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(user);
        Assert.Equal(username, user.Username);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task AuthenticatedRequest_ValidToken_AccessGranted()
    {
        // Arrange - Get a valid token
        var token = await RegisterUserAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Make an authenticated request (using validate-token as a proxy for any authenticated endpoint)
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_NoToken_AccessDenied()
    {
        // Act - Make a request without token to an endpoint that requires authentication
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task TokenReuse_SameTokenMultipleRequests_AllSucceed()
    {
        // Arrange
        var token = await RegisterUserAndGetToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Make multiple requests with the same token
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/api/auth/validate-token"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
    }

    [Fact]
    public async Task ConcurrentAuthentication_MultipleUsers_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task<string>>();

        // Act - Register multiple users concurrently
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(RegisterUserAndGetToken());
        }

        var tokens = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, tokens.Length);
        Assert.All(tokens, token => Assert.NotEmpty(token));

        // Ensure all tokens are unique
        Assert.Equal(tokens.Length, tokens.Distinct().Count());
    }

    [Fact]
    public async Task TokenValidation_DifferentCasing_Handled()
    {
        // Arrange
        var token = await RegisterUserAndGetToken();

        // Test different casing for Bearer by setting raw header values
        var testCases = new[] { "Bearer", "bearer", "BEARER", "BeArEr" };

        foreach (var bearerCase in testCases)
        {
            // Clear previous authorization header
            _client.DefaultRequestHeaders.Authorization = null;

            // Act - Set raw Authorization header with different casing
            _client.DefaultRequestHeaders.Remove("Authorization");
            _client.DefaultRequestHeaders.Add("Authorization", $"{bearerCase} {token}");

            var response = await _client.GetAsync("/api/auth/validate-token");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Clean up for next iteration
            _client.DefaultRequestHeaders.Remove("Authorization");
        }
    }

    #endregion
}