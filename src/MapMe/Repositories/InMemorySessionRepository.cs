using System.Collections.Concurrent;
using MapMe.Models;

namespace MapMe.Repositories;

/// <summary>
/// In-memory implementation of ISessionRepository for development and testing
/// </summary>
public class InMemorySessionRepository : ISessionRepository
{
    private readonly Timer _cleanupTimer;
    private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
    private readonly IUserRepository _userRepository;
    private readonly ConcurrentDictionary<string, HashSet<string>> _userSessions = new();

    public InMemorySessionRepository(IUserRepository userRepository)
    {
        _userRepository = userRepository;
        // Clean up expired sessions every 5 minutes
        _cleanupTimer = new Timer(async _ => await CleanupExpiredSessionsAsync(),
            null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<UserSession> CreateSessionAsync(string userId, TimeSpan duration)
    {
        var user = await GetUserInfoAsync(userId);
        var sessionId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        var session = new UserSession(
            UserId: userId,
            Username: user.username,
            Email: user.email,
            SessionId: sessionId,
            ExpiresAt: now.Add(duration),
            CreatedAt: now
        );

        _sessions[sessionId] = session;

        // Track user sessions
        _userSessions.AddOrUpdate(userId,
            new HashSet<string> { sessionId },
            (key, existing) =>
            {
                existing.Add(sessionId);
                return existing;
            });

        return session;
    }

    public Task<UserSession?> GetValidSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            if (session.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return Task.FromResult<UserSession?>(session);
            }
            else
            {
                // Session expired, remove it
                _sessions.TryRemove(sessionId, out _);
                RemoveFromUserSessions(session.UserId, sessionId);
            }
        }

        return Task.FromResult<UserSession?>(null);
    }

    public Task InvalidateSessionAsync(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            RemoveFromUserSessions(session.UserId, sessionId);
        }

        return Task.CompletedTask;
    }

    public async Task<UserSession?> RefreshSessionAsync(string sessionId, TimeSpan duration)
    {
        var existingSession = await GetValidSessionAsync(sessionId);
        if (existingSession == null)
        {
            return null;
        }

        // Create new session
        var newSession = await CreateSessionAsync(existingSession.UserId, duration);

        // Invalidate old session
        await InvalidateSessionAsync(sessionId);

        return newSession;
    }

    public Task CleanupExpiredSessionsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredSessions = _sessions.Where(kvp => kvp.Value.ExpiresAt <= now).ToList();

        foreach (var (sessionId, session) in expiredSessions)
        {
            _sessions.TryRemove(sessionId, out _);
            RemoveFromUserSessions(session.UserId, sessionId);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<UserSession>> GetUserSessionsAsync(string userId)
    {
        if (_userSessions.TryGetValue(userId, out var sessionIds))
        {
            var validSessions = sessionIds
                .Select(id => _sessions.TryGetValue(id, out var session) ? session : null)
                .Where(session => session != null && session.ExpiresAt > DateTimeOffset.UtcNow)
                .Cast<UserSession>();

            return Task.FromResult(validSessions);
        }

        return Task.FromResult(Enumerable.Empty<UserSession>());
    }

    public Task InvalidateAllUserSessionsAsync(string userId)
    {
        if (_userSessions.TryGetValue(userId, out var sessionIds))
        {
            foreach (var sessionId in sessionIds.ToList())
            {
                _sessions.TryRemove(sessionId, out _);
            }

            _userSessions.TryRemove(userId, out _);
        }

        return Task.CompletedTask;
    }

    private void RemoveFromUserSessions(string userId, string sessionId)
    {
        if (_userSessions.TryGetValue(userId, out var sessionIds))
        {
            sessionIds.Remove(sessionId);
            if (sessionIds.Count == 0)
            {
                _userSessions.TryRemove(userId, out _);
            }
        }
    }

    private async Task<(string username, string email)> GetUserInfoAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                return (user.Username, user.Email);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user info for session: {ex.Message}");
        }

        // Fallback to default values
        return ("user", "user@example.com");
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}