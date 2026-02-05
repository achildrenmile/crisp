using System.Collections.Concurrent;
using CRISP.Api.Models;
using CRISP.Core.Configuration;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Api.Services;

/// <summary>
/// Session manager with file-based persistence.
/// Sessions are kept in memory for fast access and persisted to disk for durability.
/// </summary>
public sealed class PersistentSessionManager : ISessionManager, IDisposable
{
    private readonly ConcurrentDictionary<string, CrispSession> _sessions = new();
    private readonly SessionPersistence _persistence;
    private readonly ILogger<PersistentSessionManager> _logger;
    private readonly Timer _saveTimer;
    private readonly HashSet<string> _dirtySessionIds = new();
    private readonly object _dirtyLock = new();

    public PersistentSessionManager(ILogger<PersistentSessionManager> logger, string? dataDirectory = null)
    {
        _logger = logger;
        var directory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".crisp", "sessions");
        _persistence = new SessionPersistence(directory);

        // Load existing sessions on startup
        LoadSessionsAsync().GetAwaiter().GetResult();

        // Save dirty sessions every 5 seconds
        _saveTimer = new Timer(SaveDirtySessions, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        _logger.LogInformation("PersistentSessionManager initialized with data directory: {Directory}", directory);
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            var persistedSessions = await _persistence.LoadAllSessionsAsync();
            _logger.LogInformation("Loading {Count} persisted sessions", persistedSessions.Count);

            foreach (var persisted in persistedSessions)
            {
                try
                {
                    var messages = persisted.Messages.Select(m => new ChatMessage(
                        m.MessageId,
                        m.Role,
                        m.Content,
                        m.Timestamp,
                        null
                    )).ToList();

                    DeliveryResult? deliveryResult = null;
                    if (persisted.DeliveryResult != null)
                    {
                        deliveryResult = new DeliveryResult
                        {
                            Success = persisted.DeliveryResult.Success,
                            Platform = persisted.DeliveryResult.Platform,
                            RepositoryUrl = persisted.DeliveryResult.RepositoryUrl,
                            CloneUrl = persisted.DeliveryResult.CloneUrl,
                            DefaultBranch = persisted.DeliveryResult.DefaultBranch,
                            PipelineUrl = persisted.DeliveryResult.PipelineUrl,
                            BuildStatus = persisted.DeliveryResult.BuildStatus,
                            VsCodeLink = persisted.DeliveryResult.VsCodeLink,
                            ErrorMessage = persisted.DeliveryResult.ErrorMessage
                        };
                    }

                    var session = CrispSession.Restore(
                        persisted.SessionId,
                        persisted.UserId,
                        persisted.ProjectName,
                        persisted.CreatedAt,
                        persisted.LastActivityAt,
                        persisted.Status,
                        messages,
                        deliveryResult
                    );

                    _sessions[session.SessionId] = session;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to restore session {SessionId}", persisted.SessionId);
                }
            }

            _logger.LogInformation("Loaded {Count} sessions successfully", _sessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load persisted sessions");
        }
    }

    public CrispSession CreateSession(string? userId = null, CrispConfiguration? configOverride = null)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..12];
        var session = new CrispSession(sessionId, userId, configOverride);
        _sessions[sessionId] = session;
        MarkDirty(sessionId);
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
        if (_sessions.TryRemove(sessionId, out _))
        {
            _persistence.DeleteSession(sessionId);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Marks a session as dirty so it will be saved on the next timer tick.
    /// Call this after modifying a session.
    /// </summary>
    public void MarkDirty(string sessionId)
    {
        lock (_dirtyLock)
        {
            _dirtySessionIds.Add(sessionId);
        }
    }

    private void SaveDirtySessions(object? state)
    {
        List<string> sessionsToSave;
        lock (_dirtyLock)
        {
            if (_dirtySessionIds.Count == 0)
                return;

            sessionsToSave = _dirtySessionIds.ToList();
            _dirtySessionIds.Clear();
        }

        foreach (var sessionId in sessionsToSave)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                try
                {
                    _persistence.SaveSessionAsync(session).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save session {SessionId}", sessionId);
                    // Re-mark as dirty to retry later
                    lock (_dirtyLock)
                    {
                        _dirtySessionIds.Add(sessionId);
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _saveTimer.Dispose();
        // Save any remaining dirty sessions
        SaveDirtySessions(null);
    }
}
