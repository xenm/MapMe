using MapMe.Models;

namespace MapMe.Repositories;

/// <summary>
/// Repository interface for user session management
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Creates a new user session
    /// </summary>
    Task<UserSession> CreateSessionAsync(string userId, TimeSpan duration);
    
    /// <summary>
    /// Gets a valid session by session ID
    /// </summary>
    Task<UserSession?> GetValidSessionAsync(string sessionId);
    
    /// <summary>
    /// Invalidates a session (logout)
    /// </summary>
    Task InvalidateSessionAsync(string sessionId);
    
    /// <summary>
    /// Refreshes an existing session with new expiration
    /// </summary>
    Task<UserSession?> RefreshSessionAsync(string sessionId, TimeSpan duration);
    
    /// <summary>
    /// Cleans up expired sessions
    /// </summary>
    Task CleanupExpiredSessionsAsync();
    
    /// <summary>
    /// Gets all active sessions for a user
    /// </summary>
    Task<IEnumerable<UserSession>> GetUserSessionsAsync(string userId);
    
    /// <summary>
    /// Invalidates all sessions for a user
    /// </summary>
    Task InvalidateAllUserSessionsAsync(string userId);
}
