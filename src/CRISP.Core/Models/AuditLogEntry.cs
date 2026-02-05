using CRISP.Core.Enums;

namespace CRISP.Core.Models;

/// <summary>
/// Represents an audit log entry for agent actions.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Unique identifier for this log entry.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// ISO 8601 timestamp of the action.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Session identifier for correlating related actions.
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// Windows Agent ID (if available).
    /// </summary>
    public string? AgentId { get; init; }

    /// <summary>
    /// The tool/operation invoked (e.g., "github-mcp.create_repository").
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Current execution phase.
    /// </summary>
    public required ExecutionPhase Phase { get; init; }

    /// <summary>
    /// Parameters passed to the action.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } =
        new Dictionary<string, object?>();

    /// <summary>
    /// Result of the action.
    /// </summary>
    public required ActionResult Result { get; init; }

    /// <summary>
    /// Short description or error message.
    /// </summary>
    public required string Detail { get; init; }

    /// <summary>
    /// Duration of the action in milliseconds.
    /// </summary>
    public long? DurationMs { get; init; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}
