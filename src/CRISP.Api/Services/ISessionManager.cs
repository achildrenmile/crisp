using CRISP.Core.Configuration;

namespace CRISP.Api.Services;

/// <summary>
/// Manages CRISP chat sessions.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Creates a new session.
    /// </summary>
    /// <param name="configOverride">Optional configuration override.</param>
    /// <returns>The created session.</returns>
    CrispSession CreateSession(CrispConfiguration? configOverride = null);

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The session, or null if not found.</returns>
    CrispSession? GetSession(string sessionId);

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    /// <returns>All active sessions.</returns>
    IReadOnlyList<CrispSession> GetAllSessions();

    /// <summary>
    /// Removes a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>True if removed, false if not found.</returns>
    bool RemoveSession(string sessionId);
}
