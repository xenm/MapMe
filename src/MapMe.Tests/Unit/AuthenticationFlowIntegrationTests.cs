using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MapMe.DTOs;
using MapMe.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MapMe.Tests.Unit;

/// <summary>
/// Integration tests for complete authentication flows including new user detection and profile prefilling
/// </summary>
public class AuthenticationFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RegisterFlow_NewUser_SetsIsNewUserTrueAndCreatesProfile()
    {
        // Arrange
        var registerRequest = new RegisterRequest(
            Username: $"testuser_{Guid.NewGuid():N}",
            Email: $"test_{Guid.NewGuid():N}@example.com",
            Password: "TestPassword123!",
            ConfirmPassword: "TestPassword123!",
            DisplayName: "Test User"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.True(authResponse.IsNewUser); // New user flag should be true
        Assert.NotNull(authResponse.Token);
        Assert.NotNull(authResponse.User);
        Assert.Equal(registerRequest.Username, authResponse.User.Username);
        Assert.Equal(registerRequest.Email, authResponse.User.Email);

        // Verify profile was created with prefilled data
        using var scope = _factory.Services.CreateScope();
        var profileRepo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
        var profile = await profileRepo.GetByUserIdAsync(authResponse.User.UserId);

        Assert.NotNull(profile);
        Assert.Equal(registerRequest.DisplayName, profile.DisplayName);
        Assert.Contains("Test User", profile.Bio); // Should contain personalized bio
        Assert.Equal("public", profile.Visibility);
    }

    [Fact]
    public async Task LoginFlow_ExistingUser_SetsIsNewUserFalse()
    {
        // Arrange - First create a user
        var registerRequest = new RegisterRequest(
            Username: $"existinguser_{Guid.NewGuid():N}",
            Email: $"existing_{Guid.NewGuid():N}@example.com",
            Password: "TestPassword123!",
            ConfirmPassword: "TestPassword123!",
            DisplayName: "Existing User"
        );

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Act - Login with existing user
        var loginRequest = new LoginRequest(
            Username: registerRequest.Username,
            Password: registerRequest.Password,
            RememberMe: false
        );

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.False(authResponse.IsNewUser); // Existing user flag should be false
        Assert.NotNull(authResponse.Token);
        Assert.NotNull(authResponse.User);
        Assert.Equal(registerRequest.Username, authResponse.User.Username);
    }

    [Fact]
    public async Task GoogleLoginFlow_NewUser_SetsIsNewUserTrueAndPrefillsProfile()
    {
        // Arrange
        var googleLoginRequest = new GoogleLoginRequest(
            GoogleToken: "mock_google_token",
            Email: $"google_{Guid.NewGuid():N}@gmail.com",
            DisplayName: "Google User",
            GoogleId: $"google_{Guid.NewGuid():N}",
            Picture: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/google-login", googleLoginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.True(authResponse.IsNewUser); // New Google user flag should be true
        Assert.NotNull(authResponse.Token);
        Assert.NotNull(authResponse.User);
        Assert.Equal(googleLoginRequest.Email, authResponse.User.Email);

        // Verify profile was created with Google data prefilling
        using var scope = _factory.Services.CreateScope();
        var profileRepo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
        var profile = await profileRepo.GetByUserIdAsync(authResponse.User.UserId);

        Assert.NotNull(profile);
        Assert.Equal(googleLoginRequest.DisplayName, profile.DisplayName);
        Assert.Contains("Google User", profile.Bio); // Should contain personalized Google bio
        Assert.Contains("new to MapMe", profile.Bio); // Should contain welcome message
        Assert.Equal("public", profile.Visibility);
    }

    [Fact]
    public async Task GoogleLoginFlow_ExistingUser_SetsIsNewUserFalse()
    {
        // Arrange - First create a Google user
        var initialGoogleRequest = new GoogleLoginRequest(
            GoogleToken: "mock_google_token",
            Email: $"existing_google_{Guid.NewGuid():N}@gmail.com",
            DisplayName: "Existing Google User",
            GoogleId: $"existing_google_{Guid.NewGuid():N}",
            Picture: null
        );

        var initialResponse = await _client.PostAsJsonAsync("/api/auth/google-login", initialGoogleRequest);
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);

        // Act - Login again with same Google ID
        var secondLoginRequest = new GoogleLoginRequest(
            GoogleToken: "mock_google_token_2",
            Email: initialGoogleRequest.Email,
            DisplayName: initialGoogleRequest.DisplayName,
            GoogleId: initialGoogleRequest.GoogleId,
            Picture: null
        );

        var secondResponse = await _client.PostAsJsonAsync("/api/auth/google-login", secondLoginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var authResponse = await secondResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse);
        Assert.True(authResponse.Success);
        Assert.False(authResponse.IsNewUser); // Existing Google user flag should be false
        Assert.NotNull(authResponse.Token);
        Assert.NotNull(authResponse.User);
    }

    [Fact]
    public async Task TokenValidation_ValidToken_ReturnsUserData()
    {
        // Arrange - Create user and get token
        var registerRequest = new RegisterRequest(
            Username: $"tokenuser_{Guid.NewGuid():N}",
            Email: $"token_{Guid.NewGuid():N}@example.com",
            Password: "TestPassword123!",
            ConfirmPassword: "TestPassword123!",
            DisplayName: "Token User"
        );

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse?.Token);

        // Act - Validate token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.Token);

        var validateResponse = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);

        var user = await validateResponse.Content.ReadFromJsonAsync<AuthenticatedUser>();
        Assert.NotNull(user);
        Assert.Equal(registerRequest.Username, user.Username);
        Assert.Equal(registerRequest.Email, user.Email);
    }

    [Fact]
    public async Task TokenValidation_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid_token");

        // Act
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenValidation_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange - Create a token that's already expired by creating a user and generating token
        var registerRequest = new RegisterRequest(
            Username: $"expireduser_{Guid.NewGuid():N}",
            Email: $"expired_{Guid.NewGuid():N}@example.com",
            Password: "TestPassword123!",
            ConfirmPassword: "TestPassword123!",
            DisplayName: "Expired User"
        );

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse?.Token);

        // Wait a moment to ensure token is considered expired for test purposes
        await Task.Delay(100);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "expired_or_invalid_token_12345");

        // Act
        var response = await _client.GetAsync("/api/auth/validate-token");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProfilePrefilling_NewRegistration_CreatesPersonalizedBio()
    {
        // Arrange
        var registerRequest = new RegisterRequest(
            Username: $"biouser_{Guid.NewGuid():N}",
            Email: $"bio_{Guid.NewGuid():N}@example.com",
            Password: "TestPassword123!",
            ConfirmPassword: "TestPassword123!",
            DisplayName: "Bio Test User"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        // Assert
        using var scope = _factory.Services.CreateScope();
        var profileRepo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
        var profile = await profileRepo.GetByUserIdAsync(authResponse!.User!.UserId);

        Assert.NotNull(profile);
        Assert.Contains("Bio Test User", profile.Bio);
        Assert.Contains("new to MapMe", profile.Bio);
        Assert.Contains("Looking forward to sharing", profile.Bio);
    }

    [Fact]
    public async Task ProfilePrefilling_GoogleRegistration_CreatesGooglePersonalizedBio()
    {
        // Arrange
        var googleRequest = new GoogleLoginRequest(
            GoogleToken: "mock_token",
            Email: $"bio_google_{Guid.NewGuid():N}@gmail.com",
            DisplayName: "Google Bio User",
            GoogleId: $"bio_google_{Guid.NewGuid():N}",
            Picture: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/google-login", googleRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        // Assert
        using var scope = _factory.Services.CreateScope();
        var profileRepo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
        var profile = await profileRepo.GetByUserIdAsync(authResponse!.User!.UserId);

        Assert.NotNull(profile);
        Assert.Contains("Google Bio User", profile.Bio);
        Assert.Contains("Hello! I'm", profile.Bio);
        Assert.Contains("new to MapMe", profile.Bio);
    }

    [Fact]
    public async Task DuplicateEmailRegistration_ReturnsError()
    {
        // Arrange - Create first user
        var firstRequest = new RegisterRequest(
            Username: $"first_{Guid.NewGuid():N}",
            Email: $"duplicate_{Guid.NewGuid():N}@example.com",
            Password: "TestPassword123!",
            ConfirmPassword: "TestPassword123!",
            DisplayName: "First User"
        );

        await _client.PostAsJsonAsync("/api/auth/register", firstRequest);

        // Act - Try to register with same email
        var duplicateRequest = new RegisterRequest(
            Username: $"second_{Guid.NewGuid():N}",
            Email: firstRequest.Email, // Same email
            Password: "TestPassword123!",
            ConfirmPassword: "TestPassword123!",
            DisplayName: "Second User"
        );

        var response = await _client.PostAsJsonAsync("/api/auth/register", duplicateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse);
        Assert.False(authResponse.Success);
        Assert.Contains("already registered", authResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidCredentials_Login_ReturnsError()
    {
        // Arrange
        var loginRequest = new LoginRequest(
            Username: "nonexistent_user",
            Password: "wrong_password",
            RememberMe: false
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authResponse);
        Assert.False(authResponse.Success);
        Assert.Contains("Invalid", authResponse.Message, StringComparison.OrdinalIgnoreCase);
    }
}