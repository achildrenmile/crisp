using System.Collections.Concurrent;
using CRISP.Core.Configuration;

namespace CRISP.Api.Services;

/// <summary>
/// In-memory session manager for MVP. Replace with Redis/SQL for production.
/// </summary>
public sealed class InMemorySessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<string, CrispSession> _sessions = new();

    public CrispSession CreateSession(string? userId = null, CrispConfiguration? configOverride = null)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..12];
        var session = new CrispSession(sessionId, userId, configOverride);
        _sessions[sessionId] = session;
        return session;
    }

    public CrispSession? GetSession(string sessionId)
    {
        return _sessions.GetValueOrDefault(sessionId);
    }

    public IReadOnlyList<CrispSession> GetAllSessions()
    {
        return _sessions.Values.ToList();
    }

    public IReadOnlyList<CrispSession> GetSessionsByUser(string userId)
    {
        return _sessions.Values
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();
    }

    public bool RemoveSession(string sessionId)
    {
        return _sessions.TryRemove(sessionId, out _);
    }
}
