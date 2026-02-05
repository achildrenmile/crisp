using System.ComponentModel;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using ModelContextProtocol.Server;

namespace CRISP.Mcp.AuditLog.Tools;

/// <summary>
/// MCP tools for audit logging operations.
/// </summary>
[McpServerToolType]
public sealed class AuditLogTools
{
    private readonly IAuditLogger _auditLogger;

    public AuditLogTools(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    [McpServerTool("log_action")]
    [Description("Logs an action to the audit log")]
    public async Task<LogResult> LogActionAsync(
        [Description("Action name (e.g., 'github-mcp.create_repository')")] string action,
        [Description("Execution phase (Intake, Planning, Execution, Delivery)")] string phase,
        [Description("Result (Success, Failure, Skipped)")] string result,
        [Description("Detail message")] string detail)
    {
        var phaseEnum = Enum.Parse<ExecutionPhase>(phase, ignoreCase: true);
        var resultEnum = Enum.Parse<ActionResult>(result, ignoreCase: true);

        await _auditLogger.LogActionAsync(action, phaseEnum, resultEnum, detail);

        return new LogResult
        {
            Success = true,
            SessionId = _auditLogger.SessionId,
            Message = "Action logged"
        };
    }

    [McpServerTool("get_session_logs")]
    [Description("Retrieves audit logs for a session")]
    public async Task<SessionLogsResult> GetSessionLogsAsync(
        [Description("Session ID (leave empty for current session)")] string? sessionId = null)
    {
        var id = string.IsNullOrEmpty(sessionId)
            ? _auditLogger.SessionId
            : Guid.Parse(sessionId);

        var logs = await _auditLogger.GetSessionLogsAsync(id);

        return new SessionLogsResult
        {
            SessionId = id,
            LogCount = logs.Count,
            Logs = logs.Select(l => new AuditLogSummary
            {
                Timestamp = l.Timestamp,
                Action = l.Action,
                Phase = l.Phase.ToString(),
                Result = l.Result.ToString(),
                Detail = l.Detail
            }).ToList()
        };
    }

    [McpServerTool("export_logs")]
    [Description("Exports audit logs to a specified format")]
    public async Task<ExportResult> ExportLogsAsync(
        [Description("Export format (json, csv)")] string format,
        [Description("Session ID (leave empty for current session)")] string? sessionId = null)
    {
        var id = string.IsNullOrEmpty(sessionId)
            ? _auditLogger.SessionId
            : Guid.Parse(sessionId);

        var content = await _auditLogger.ExportLogsAsync(id, format);

        return new ExportResult
        {
            SessionId = id,
            Format = format,
            Content = content
        };
    }

    [McpServerTool("get_session_id")]
    [Description("Gets the current session ID")]
    public SessionIdResult GetSessionId()
    {
        return new SessionIdResult { SessionId = _auditLogger.SessionId };
    }

    [McpServerTool("set_agent_id")]
    [Description("Sets the agent ID for audit logging")]
    public SetAgentIdResult SetAgentId(
        [Description("Agent ID")] string agentId)
    {
        _auditLogger.SetAgentId(agentId);
        return new SetAgentIdResult { Success = true, AgentId = agentId };
    }
}

public sealed record LogResult
{
    public required bool Success { get; init; }
    public required Guid SessionId { get; init; }
    public required string Message { get; init; }
}

public sealed record SessionLogsResult
{
    public required Guid SessionId { get; init; }
    public required int LogCount { get; init; }
    public required IReadOnlyList<AuditLogSummary> Logs { get; init; }
}

public sealed record AuditLogSummary
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Action { get; init; }
    public required string Phase { get; init; }
    public required string Result { get; init; }
    public required string Detail { get; init; }
}

public sealed record ExportResult
{
    public required Guid SessionId { get; init; }
    public required string Format { get; init; }
    public required string Content { get; init; }
}

public sealed record SessionIdResult
{
    public required Guid SessionId { get; init; }
}

public sealed record SetAgentIdResult
{
    public required bool Success { get; init; }
    public required string AgentId { get; init; }
}
