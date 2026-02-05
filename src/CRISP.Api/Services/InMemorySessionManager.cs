using System.Collections.Concurrent;
using CRISP.Core.Configuration;

namespace CRISP.Api.Services;

/// <summary>
/// In-memory session manager for MVP. Replace with Redis/SQL for production.
/// </summary>
public sealed class InMemorySessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<string, CrispSession> _sessions = new();

    public CrispSession CreateSession(CrispConfiguration? configOverride = null)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..12];
        var session = new CrispSession(sessionId, configOverride);
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

    public bool RemoveSession(string sessionId)
    {
        return _sessions.TryRemove(sessionId, out _);
    }
}
