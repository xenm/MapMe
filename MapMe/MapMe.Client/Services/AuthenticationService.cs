using MapMe.Client.DTOs;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace MapMe.Client.Services;

/// <summary>
/// Client-side authentication service for Blazor components
/// </summary>
public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private AuthenticatedUser? _currentUser;
    private string? _sessionId;

    public AuthenticationService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Event fired when authentication state changes
    /// </summary>
    public event Action<AuthenticatedUser?>? AuthenticationStateChanged;

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    public AuthenticatedUser? CurrentUser => _currentUser;

    /// <summary>
    /// Gets whether a user is currently authenticated
    /// </summary>
    public bool IsAuthenticated => _currentUser != null;

    /// <summary>
    /// Gets the current session ID
    /// </summary>
    public string? SessionId => _sessionId;

    /// <summary>
    /// Initializes the authentication service by checking for existing session
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var sessionId = await GetStoredSessionIdAsync();
            if (!string.IsNullOrEmpty(sessionId))
            {
                // First, optimistically restore the session
                _sessionId = sessionId;
                SetAuthorizationHeader();
                
                // Then validate in the background
                try
                {
                    var user = await ValidateSessionAsync(sessionId);
                    if (user != null)
                    {
                        _currentUser = user;
                        NotifyAuthenticationStateChanged();
                    }
                    else
                    {
                        // Only clear if validation explicitly fails
                        _currentUser = null;
                        _sessionId = null;
                        await ClearStoredSessionAsync();
                        NotifyAuthenticationStateChanged();
                    }
                }
                catch (Exception validationEx)
                {
                    Console.WriteLine($"Session validation failed, but keeping session for retry: {validationEx.Message}");
                    // Don't clear session on validation error - might be temporary network issue
                    // Create a minimal user object to maintain authentication state
                    _currentUser = new AuthenticatedUser
                    {
                        UserId = "temp_user",
                        Username = "User",
                        Email = "user@temp.com",
                        DisplayName = "User"
                    };
                    NotifyAuthenticationStateChanged();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing authentication: {ex.Message}");
            // Don't clear session on initialization error
        }
    }

    /// <summary>
    /// Logs in a user with username and password
    /// </summary>
    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            
            if (result != null && result.Success && result.User != null && result.SessionId != null)
            {
                _currentUser = result.User;
                _sessionId = result.SessionId;
                await StoreSessionIdAsync(result.SessionId);
                NotifyAuthenticationStateChanged();
            }

            return result ?? new AuthenticationResponse { Success = false, Message = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during login: {ex.Message}");
            return new AuthenticationResponse { Success = false, Message = "An error occurred during login" };
        }
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    public async Task<AuthenticationResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            
            if (result != null && result.Success && result.User != null && result.SessionId != null)
            {
                _currentUser = result.User;
                _sessionId = result.SessionId;
                await StoreSessionIdAsync(result.SessionId);
                NotifyAuthenticationStateChanged();
            }

            return result ?? new AuthenticationResponse { Success = false, Message = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during registration: {ex.Message}");
            return new AuthenticationResponse { Success = false, Message = "An error occurred during registration" };
        }
    }

    /// <summary>
    /// Logs in a user with Google OAuth
    /// </summary>
    public async Task<AuthenticationResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/google-login", request);
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            
            if (result != null && result.Success && result.User != null && result.SessionId != null)
            {
                _currentUser = result.User;
                _sessionId = result.SessionId;
                await StoreSessionIdAsync(result.SessionId);
                NotifyAuthenticationStateChanged();
            }

            return result ?? new AuthenticationResponse { Success = false, Message = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during Google login: {ex.Message}");
            return new AuthenticationResponse { Success = false, Message = "An error occurred during Google login" };
        }
    }

    /// <summary>
    /// Logs out the current user
    /// </summary>
    public async Task<bool> LogoutAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_sessionId))
            {
                var request = new LogoutRequest { SessionId = _sessionId };
                await _httpClient.PostAsJsonAsync("/api/auth/logout", request);
            }

            _currentUser = null;
            _sessionId = null;
            await ClearStoredSessionAsync();
            NotifyAuthenticationStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during logout: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Changes the current user's password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("/api/auth/change-password", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing password: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Requests a password reset for the given email
    /// </summary>
    public async Task<bool> RequestPasswordResetAsync(PasswordResetRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/password-reset", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting password reset: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validates the current session
    /// </summary>
    public async Task<AuthenticatedUser?> ValidateSessionAsync(string sessionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/auth/validate-session?sessionId={sessionId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthenticatedUser>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating session: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Refreshes the current session
    /// </summary>
    public async Task<bool> RefreshSessionAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh-session", new { SessionId = _sessionId });
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            
            if (result != null && result.Success && result.SessionId != null)
            {
                _sessionId = result.SessionId;
                await StoreSessionIdAsync(result.SessionId);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing session: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the authorization header value for API requests
    /// </summary>
    public string? GetAuthorizationHeader()
    {
        return string.IsNullOrEmpty(_sessionId) ? null : $"Bearer {_sessionId}";
    }

    /// <summary>
    /// Sets the authorization header for HTTP requests
    /// </summary>
    public void SetAuthorizationHeader()
    {
        var authHeader = GetAuthorizationHeader();
        if (!string.IsNullOrEmpty(authHeader))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sessionId);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    #region Private Methods

    private void NotifyAuthenticationStateChanged()
    {
        AuthenticationStateChanged?.Invoke(_currentUser);
        SetAuthorizationHeader();
    }

    private async Task<string?> GetStoredSessionIdAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "mapme_session_id");
        }
        catch
        {
            return null;
        }
    }

    private async Task StoreSessionIdAsync(string sessionId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "mapme_session_id", sessionId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing session ID: {ex.Message}");
        }
    }

    private async Task ClearStoredSessionAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "mapme_session_id");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing stored session: {ex.Message}");
        }
    }

    #endregion
}
