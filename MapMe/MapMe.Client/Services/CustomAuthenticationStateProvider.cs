using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using MapMe.Client.DTOs;

namespace MapMe.Client.Services;

/// <summary>
/// Custom authentication state provider for MapMe application
/// </summary>
public class CustomAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthenticationService _authService;
    private AuthenticationState _currentAuthenticationState;

    public CustomAuthenticationStateProvider(AuthenticationService authService)
    {
        _authService = authService;
        _currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        
        // Subscribe to authentication state changes
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    /// <summary>
    /// Gets the current authentication state
    /// </summary>
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Check if user is already authenticated
        if (_authService.IsAuthenticated)
        {
            var user = _authService.CurrentUser;
            if (user != null)
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
                return Task.FromResult(_currentAuthenticationState);
            }
        }

        // Try to restore session from storage
        try
        {
            // Check if user is already restored during service initialization
            if (_authService.IsAuthenticated && _authService.CurrentUser != null)
            {
                var user = _authService.CurrentUser;
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
                return Task.FromResult(_currentAuthenticationState);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error restoring session: {ex.Message}");
        }

        // Return unauthenticated state
        _currentAuthenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        return Task.FromResult(_currentAuthenticationState);
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

    /// <summary>
    /// Dispose of event subscriptions
    /// </summary>
    public void Dispose()
    {
        _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        GC.SuppressFinalize(this);
    }
}
