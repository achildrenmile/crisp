using System.Text.Json;
using System.Text.Json.Serialization;
using CRISP.Api.Models;
using CRISP.Core.Models;

namespace CRISP.Api.Services;

/// <summary>
/// DTO for persisting session data to JSON.
/// </summary>
public sealed class PersistedSession
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? ProjectName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public SessionStatus Status { get; set; }
    public List<PersistedMessage> Messages { get; set; } = new();
    public PersistedDeliveryResult? DeliveryResult { get; set; }
}

public sealed class PersistedMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public sealed class PersistedDeliveryResult
{
    public bool Success { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string RepositoryUrl { get; set; } = string.Empty;
    public string CloneUrl { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = string.Empty;
    public string? PipelineUrl { get; set; }
    public string? BuildStatus { get; set; }
    public string VsCodeLink { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Handles reading and writing session data to disk.
/// </summary>
public sealed class SessionPersistence
{
    private readonly string _dataDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public SessionPersistence(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
        Directory.CreateDirectory(_dataDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task SaveSessionAsync(CrispSession session)
    {
        var persisted = new PersistedSession
        {
            SessionId = session.SessionId,
            UserId = session.UserId,
            ProjectName = session.ProjectName,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            Status = session.Status,
            Messages = session.Messages.Select(m => new PersistedMessage
            {
                MessageId = m.MessageId,
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList()
        };

        if (session.DeliveryResult != null)
        {
            persisted.DeliveryResult = new PersistedDeliveryResult
            {
                Success = session.DeliveryResult.Success,
                Platform = session.DeliveryResult.Platform,
                RepositoryUrl = session.DeliveryResult.RepositoryUrl,
                CloneUrl = session.DeliveryResult.CloneUrl,
                DefaultBranch = session.DeliveryResult.DefaultBranch,
                PipelineUrl = session.DeliveryResult.PipelineUrl,
                BuildStatus = session.DeliveryResult.BuildStatus,
                VsCodeLink = session.DeliveryResult.VsCodeLink,
                ErrorMessage = session.DeliveryResult.ErrorMessage
            };
        }

        var filePath = GetSessionFilePath(session.SessionId);
        var json = JsonSerializer.Serialize(persisted, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<List<PersistedSession>> LoadAllSessionsAsync()
    {
        var sessions = new List<PersistedSession>();

        if (!Directory.Exists(_dataDirectory))
            return sessions;

        foreach (var file in Directory.GetFiles(_dataDirectory, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var session = JsonSerializer.Deserialize<PersistedSession>(json, _jsonOptions);
                if (session != null)
                {
                    sessions.Add(session);
                }
            }
            catch
            {
                // Skip corrupted files
            }
        }

        return sessions;
    }

    public void DeleteSession(string sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetSessionFilePath(string sessionId)
    {
        return Path.Combine(_dataDirectory, $"{sessionId}.json");
    }
}
