using CRISP.Core.Enums;
using CRISP.Core.Models;

namespace CRISP.Core.Interfaces;

/// <summary>
/// Audit logging service for tracking agent actions.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an action to the audit log.
    /// </summary>
    /// <param name="entry">Audit log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an action with automatic timestamp and session tracking.
    /// </summary>
    /// <param name="action">Action name.</param>
    /// <param name="phase">Execution phase.</param>
    /// <param name="result">Action result.</param>
    /// <param name="detail">Detail message.</param>
    /// <param name="parameters">Action parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogActionAsync(
        string action,
        ExecutionPhase phase,
        ActionResult result,
        string detail,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit log entries for a session.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries.</returns>
    Task<IReadOnlyList<AuditLogEntry>> GetSessionLogsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs to a specified format.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="format">Export format (json, csv).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exported content.</returns>
    Task<string> ExportLogsAsync(
        Guid sessionId,
        string format,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current session identifier.
    /// </summary>
    Guid SessionId { get; }

    /// <summary>
    /// Sets the agent identifier for audit logging.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    void SetAgentId(string agentId);
}
