using System.Collections.Concurrent;
using System.Text.Json;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;

namespace CRISP.Audit;

/// <summary>
/// Implementation of audit logging service.
/// </summary>
public sealed class AuditLogger : IAuditLogger, IDisposable
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly ConcurrentDictionary<Guid, List<AuditLogEntry>> _sessionLogs = new();
    private readonly object _lock = new();
    private string? _agentId;
    private bool _disposed;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
        SessionId = Guid.NewGuid();
        _sessionLogs[SessionId] = [];
    }

    public Guid SessionId { get; }

    public void SetAgentId(string agentId)
    {
        _agentId = agentId;
    }

    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (!_sessionLogs.TryGetValue(entry.SessionId, out var logs))
            {
                logs = [];
                _sessionLogs[entry.SessionId] = logs;
            }

            logs.Add(entry);
        }

        // Log to structured logging as well
        _logger.LogInformation(
            "Audit: {Action} | Phase: {Phase} | Result: {Result} | Detail: {Detail}",
            entry.Action,
            entry.Phase,
            entry.Result,
            entry.Detail);

        return Task.CompletedTask;
    }

    public async Task LogActionAsync(
        string action,
        ExecutionPhase phase,
        ActionResult result,
        string detail,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            SessionId = SessionId,
            AgentId = _agentId,
            Action = action,
            Phase = phase,
            Result = result,
            Detail = detail,
            Parameters = parameters ?? new Dictionary<string, object?>()
        };

        await LogAsync(entry, cancellationToken);
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetSessionLogsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_sessionLogs.TryGetValue(sessionId, out var logs))
            {
                return Task.FromResult<IReadOnlyList<AuditLogEntry>>(logs.ToList());
            }

            return Task.FromResult<IReadOnlyList<AuditLogEntry>>([]);
        }
    }

    public async Task<string> ExportLogsAsync(
        Guid sessionId,
        string format,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetSessionLogsAsync(sessionId, cancellationToken);

        return format.ToLowerInvariant() switch
        {
            "json" => ExportToJson(logs),
            "csv" => ExportToCsv(logs),
            _ => throw new ArgumentException($"Unsupported export format: {format}", nameof(format))
        };
    }

    private static string ExportToJson(IReadOnlyList<AuditLogEntry> logs)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(logs, options);
    }

    private static string ExportToCsv(IReadOnlyList<AuditLogEntry> logs)
    {
        var lines = new List<string>
        {
            "Id,Timestamp,SessionId,AgentId,Action,Phase,Result,Detail,DurationMs"
        };

        foreach (var log in logs)
        {
            var line = string.Join(",",
                EscapeCsv(log.Id.ToString()),
                EscapeCsv(log.Timestamp.ToString("O")),
                EscapeCsv(log.SessionId.ToString()),
                EscapeCsv(log.AgentId ?? ""),
                EscapeCsv(log.Action),
                EscapeCsv(log.Phase.ToString()),
                EscapeCsv(log.Result.ToString()),
                EscapeCsv(log.Detail),
                log.DurationMs?.ToString() ?? "");

            lines.Add(line);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sessionLogs.Clear();
    }
}
