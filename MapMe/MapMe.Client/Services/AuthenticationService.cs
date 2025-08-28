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
    private string? _jwtToken;

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
    /// Gets the current JWT token
    /// </summary>
    public string? Token => _jwtToken;

    /// <summary>
    /// Initializes the authentication service by checking for existing JWT token
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            var token = await GetStoredTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                // First, optimistically restore the token
                _jwtToken = token;
                SetAuthorizationHeader();
                
                // Then validate in the background
                try
                {
                    var user = await ValidateTokenAsync(token);
                    if (user != null)
                    {
                        _currentUser = user;
                        NotifyAuthenticationStateChanged();
                    }
                    else
                    {
                        // Only clear if validation explicitly fails
                        _currentUser = null;
                        _jwtToken = null;
                        await ClearStoredTokenAsync();
                        NotifyAuthenticationStateChanged();
                    }
                }
                catch (Exception validationEx)
                {
                    Console.WriteLine($"Token validation failed, but keeping token for retry: {validationEx.Message}");
                    // Don't clear token on validation error - might be temporary network issue
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
            // Don't clear token on initialization error
        }
    }

    /// <summary>
    /// Logs in a user with username and password
    /// </summary>
    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            Console.WriteLine($"[DEBUG] LoginAsync called for user: {request.Username}");
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            Console.WriteLine($"[DEBUG] Login response status: {response.StatusCode}");
            
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Console.WriteLine($"[DEBUG] Login result - Success: {result?.Success}, HasToken: {!string.IsNullOrEmpty(result?.Token)}");
            
            if (result != null && result.Success && result.User != null && result.Token != null)
            {
                Console.WriteLine($"[DEBUG] Setting up authentication - Token: {result.Token.Substring(0, Math.Min(20, result.Token.Length))}...");
                _currentUser = result.User;
                _jwtToken = result.Token;
                await StoreTokenAsync(result.Token);
                SetAuthorizationHeader();
                Console.WriteLine($"[DEBUG] Authorization header set after login");
                NotifyAuthenticationStateChanged();
            }
            else if (result != null)
            {
                Console.WriteLine($"[DEBUG] Login failed: {result.Message}");
            }

            return result ?? new AuthenticationResponse { Success = false, Message = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error during login: {ex.Message}");
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
            Console.WriteLine($"[DEBUG] RegisterAsync called for user: {request.Username}");
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            Console.WriteLine($"[DEBUG] Registration response status: {response.StatusCode}");
            
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            Console.WriteLine($"[DEBUG] Registration result - Success: {result?.Success}, HasToken: {!string.IsNullOrEmpty(result?.Token)}");
            
            if (result != null && result.Success && result.User != null && result.Token != null)
            {
                Console.WriteLine($"[DEBUG] Setting up authentication after registration - Token: {result.Token.Substring(0, Math.Min(20, result.Token.Length))}...");
                _currentUser = result.User;
                _jwtToken = result.Token;
                await StoreTokenAsync(result.Token);
                SetAuthorizationHeader();
                Console.WriteLine($"[DEBUG] Authorization header set after registration");
                NotifyAuthenticationStateChanged();
            }
            else if (result != null)
            {
                Console.WriteLine($"[DEBUG] Registration failed: {result.Message}");
            }

            return result ?? new AuthenticationResponse { Success = false, Message = "Invalid response from server" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error during registration: {ex.Message}");
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
            
            if (result != null && result.Success && result.User != null && result.Token != null)
            {
                _currentUser = result.User;
                _jwtToken = result.Token;
                await StoreTokenAsync(result.Token);
                SetAuthorizationHeader();
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
            if (!string.IsNullOrEmpty(_jwtToken))
            {
                var request = new LogoutRequest { Token = _jwtToken };
                await _httpClient.PostAsJsonAsync("/api/auth/logout", request);
            }

            _currentUser = null;
            _jwtToken = null;
            await ClearStoredTokenAsync();
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
            if (string.IsNullOrEmpty(_jwtToken))
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
            if (string.IsNullOrEmpty(_jwtToken))
            {
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh-token", new { Token = _jwtToken });
            var result = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
            
            if (result != null && result.Success && result.Token != null)
            {
                _jwtToken = result.Token;
                await StoreTokenAsync(result.Token);
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
        return string.IsNullOrEmpty(_jwtToken) ? null : $"Bearer {_jwtToken}";
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
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    #region Private Methods

    /// <summary>
    /// Validates a JWT token with the server and returns user info
    /// </summary>
    private async Task<AuthenticatedUser?> ValidateTokenAsync(string token)
    {
        try
        {
            Console.WriteLine($"[DEBUG] ValidateTokenAsync called with token: {token.Substring(0, Math.Min(20, token?.Length ?? 0))}...");
            
            // Set the Authorization header for this request
            var previousAuth = _httpClient.DefaultRequestHeaders.Authorization;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            Console.WriteLine($"[DEBUG] Authorization header set: Bearer {token.Substring(0, Math.Min(20, token?.Length ?? 0))}...");
            
            try
            {
                Console.WriteLine($"[DEBUG] Making GET request to /api/auth/validate-token");
                var response = await _httpClient.GetAsync("/api/auth/validate-token");
                Console.WriteLine($"[DEBUG] Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthenticatedUser>();
                    Console.WriteLine($"[DEBUG] Token validation successful, user: {result?.Username}");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DEBUG] Token validation failed: {response.StatusCode} - {errorContent}");
                }
                return null;
            }
            finally
            {
                // Restore previous authorization header
                _httpClient.DefaultRequestHeaders.Authorization = previousAuth;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error validating token: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    private void NotifyAuthenticationStateChanged()
    {
        AuthenticationStateChanged?.Invoke(_currentUser);
        SetAuthorizationHeader();
    }

    private async Task<string?> GetStoredTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "mapme_jwt_token");
        }
        catch
        {
            return null;
        }
    }

    private async Task StoreTokenAsync(string token)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "mapme_jwt_token", token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing JWT token: {ex.Message}");
        }
    }

    private async Task ClearStoredTokenAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "mapme_jwt_token");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing stored token: {ex.Message}");
        }
    }

    #endregion
}
