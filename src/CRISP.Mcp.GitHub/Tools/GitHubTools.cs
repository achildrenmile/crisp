using System.ComponentModel;
using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace CRISP.Mcp.GitHub.Tools;

/// <summary>
/// MCP tools for GitHub operations.
/// </summary>
[McpServerToolType]
public sealed class GitHubTools
{
    private readonly ISourceControlProvider _sourceControl;
    private readonly CrispConfiguration _config;

    public GitHubTools(
        ISourceControlProvider sourceControl,
        IOptions<CrispConfiguration> config)
    {
        _sourceControl = sourceControl;
        _config = config.Value;
    }

    [McpServerTool(Name = "create_repository")]
    [Description("Creates a new GitHub repository")]
    public async Task<CreateRepositoryResult> CreateRepositoryAsync(
        [Description("Repository name")] string name,
        [Description("Repository description")] string? description = null,
        [Description("Visibility: public, private, or internal")] string visibility = "private")
    {
        var visibilityEnum = visibility.ToLowerInvariant() switch
        {
            "public" => RepositoryVisibility.Public,
            "internal" => RepositoryVisibility.Internal,
            _ => RepositoryVisibility.Private
        };

        var result = await _sourceControl.CreateRepositoryAsync(name, description, visibilityEnum);

        return new CreateRepositoryResult
        {
            Success = true,
            Name = result.Name,
            Url = result.Url ?? string.Empty,
            CloneUrl = result.CloneUrl ?? string.Empty,
            DefaultBranch = result.DefaultBranch
        };
    }

    [McpServerTool(Name = "configure_branch_protection")]
    [Description("Configures branch protection rules on a repository")]
    public async Task<BranchProtectionResult> ConfigureBranchProtectionAsync(
        [Description("Repository name")] string repositoryName,
        [Description("Branch name to protect")] string branchName = "main")
    {
        await _sourceControl.ConfigureBranchProtectionAsync(repositoryName, branchName);

        return new BranchProtectionResult
        {
            Success = true,
            Message = $"Branch protection configured for {branchName}"
        };
    }

    [McpServerTool(Name = "trigger_workflow")]
    [Description("Triggers a GitHub Actions workflow")]
    public async Task<WorkflowTriggerResult> TriggerWorkflowAsync(
        [Description("Repository name")] string repositoryName,
        [Description("Branch name")] string branchName = "main",
        [Description("Workflow ID or filename")] string? workflowId = null)
    {
        var (runUrl, runId) = await _sourceControl.TriggerPipelineAsync(
            repositoryName, workflowId, branchName);

        return new WorkflowTriggerResult
        {
            Success = true,
            RunUrl = runUrl,
            RunId = runId
        };
    }

    [McpServerTool(Name = "get_workflow_status")]
    [Description("Gets the status of a GitHub Actions workflow run")]
    public async Task<WorkflowStatusResult> GetWorkflowStatusAsync(
        [Description("Repository name")] string repositoryName,
        [Description("Workflow run ID")] string runId)
    {
        var (status, conclusion) = await _sourceControl.GetPipelineStatusAsync(repositoryName, runId);

        return new WorkflowStatusResult
        {
            RunId = runId,
            Status = status,
            Conclusion = conclusion
        };
    }

    [McpServerTool(Name = "validate_connection")]
    [Description("Validates the GitHub connection and authentication")]
    public async Task<ConnectionValidationResult> ValidateConnectionAsync()
    {
        var isValid = await _sourceControl.ValidateConnectionAsync();
        string? username = null;

        if (isValid)
        {
            username = await _sourceControl.GetAuthenticatedUserAsync();
        }

        return new ConnectionValidationResult
        {
            IsValid = isValid,
            Username = username,
            Owner = _config.GitHub.Owner
        };
    }
}

public sealed record CreateRepositoryResult
{
    public required bool Success { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string CloneUrl { get; init; }
    public required string DefaultBranch { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record BranchProtectionResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
}

public sealed record WorkflowTriggerResult
{
    public required bool Success { get; init; }
    public required string RunUrl { get; init; }
    public required string RunId { get; init; }
}

public sealed record WorkflowStatusResult
{
    public required string RunId { get; init; }
    public required string Status { get; init; }
    public string? Conclusion { get; init; }
}

public sealed record ConnectionValidationResult
{
    public required bool IsValid { get; init; }
    public string? Username { get; init; }
    public string? Owner { get; init; }
}
