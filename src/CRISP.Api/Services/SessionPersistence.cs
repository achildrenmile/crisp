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
    // Legacy field - kept for backwards compatibility during migration
    public string? VsCodeLink { get; set; }
    public string VsCodeWebUrl { get; set; } = string.Empty;
    public string VsCodeCloneUrl { get; set; } = string.Empty;
    public string SummaryCard { get; set; } = string.Empty;
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
                VsCodeWebUrl = session.DeliveryResult.VsCodeWebUrl,
                VsCodeCloneUrl = session.DeliveryResult.VsCodeCloneUrl,
                SummaryCard = session.DeliveryResult.SummaryCard,
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
                    // Migrate old vscode:// protocol links to vscode.dev URLs
                    MigrateVsCodeLink(session);
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

    /// <summary>
    /// Migrates old session data to new format with both VS Code URLs.
    /// </summary>
    private static void MigrateVsCodeLink(PersistedSession session)
    {
        if (session.DeliveryResult == null)
            return;

        var repoUrl = session.DeliveryResult.RepositoryUrl;
        var cloneUrl = session.DeliveryResult.CloneUrl;

        if (string.IsNullOrEmpty(repoUrl))
            return;

        // Always ensure VsCodeCloneUrl is set
        if (string.IsNullOrEmpty(session.DeliveryResult.VsCodeCloneUrl) && !string.IsNullOrEmpty(cloneUrl))
        {
            session.DeliveryResult.VsCodeCloneUrl = $"vscode://vscode.git/clone?url={Uri.EscapeDataString(cloneUrl)}";
        }

        // Always ensure VsCodeWebUrl is set
        if (string.IsNullOrEmpty(session.DeliveryResult.VsCodeWebUrl))
        {
            // Check if we have old VsCodeLink that's already a web URL
            var oldLink = session.DeliveryResult.VsCodeLink;
            if (!string.IsNullOrEmpty(oldLink) && oldLink.StartsWith("https://"))
            {
                session.DeliveryResult.VsCodeWebUrl = oldLink;
            }
            else
            {
                // Generate from repository URL
                if (repoUrl.Contains("github.com"))
                {
                    var uri = new Uri(repoUrl);
                    var path = uri.AbsolutePath.TrimStart('/').TrimEnd('/');
                    if (path.EndsWith(".git"))
                        path = path[..^4];
                    session.DeliveryResult.VsCodeWebUrl = $"https://vscode.dev/github/{path}";
                }
                else if (repoUrl.Contains("dev.azure.com") || repoUrl.Contains("visualstudio.com"))
                {
                    session.DeliveryResult.VsCodeWebUrl = $"{repoUrl}?path=/&_a=contents";
                }
                else
                {
                    session.DeliveryResult.VsCodeWebUrl = repoUrl;
                }
            }
        }
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
