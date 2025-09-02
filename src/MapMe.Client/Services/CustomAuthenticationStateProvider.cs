using System.Security.Claims;
using MapMe.Client.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace MapMe.Client.Services;

/// <summary>
/// Custom authentication state provider for MapMe application
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthenticationService _authService;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private AuthenticationState _currentAuthenticationState;
    private bool _isInitialized = false;

    public CustomAuthenticationStateProvider(AuthenticationService authService)
    {
        _authService = authService;
        _currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        // Subscribe to authentication state changes
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    /// <summary>
    /// Dispose of event subscriptions and resources
    /// </summary>
    public void Dispose()
    {
        _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        _initializationSemaphore?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the current authentication state
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_isInitialized)
        {
            await _initializationSemaphore.WaitAsync();
            try
            {
                if (!_isInitialized)
                {
                    await _authService.InitializeAsync();
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing auth service: {ex.Message}");
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        return _currentAuthenticationState;
    }

    /// <summary>
    /// Marks the user as authenticated
    /// </summary>
    public void MarkUserAsAuthenticated(AuthenticatedUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("DisplayName", user.DisplayName ?? user.Username)
        };

        var identity = new ClaimsIdentity(claims, "custom");
        var principal = new ClaimsPrincipal(identity);
        _currentAuthenticationState = new AuthenticationState(principal);

        NotifyAuthenticationStateChanged(Task.FromResult(_currentAuthenticationState));
    }

    /// <summary>
    /// Marks the user as logged out
    /// </summary>
    public void MarkUserAsLoggedOut()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        _currentAuthenticationState = new AuthenticationState(principal);

        NotifyAuthenticationStateChanged(Task.FromResult(_currentAuthenticationState));
    }

    /// <summary>
    /// Handles authentication state changes from the authentication service
    /// </summary>
    private void OnAuthenticationStateChanged(AuthenticatedUser? user)
    {
        if (user != null)
        {
            MarkUserAsAuthenticated(user);
        }
        else
        {
            MarkUserAsLoggedOut();
        }
    }
}