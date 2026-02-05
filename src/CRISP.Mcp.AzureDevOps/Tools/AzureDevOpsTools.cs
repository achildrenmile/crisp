using System.ComponentModel;
using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace CRISP.Mcp.AzureDevOps.Tools;

/// <summary>
/// MCP tools for Azure DevOps Server operations.
/// </summary>
[McpServerToolType]
public sealed class AzureDevOpsTools
{
    private readonly ISourceControlProvider _sourceControl;
    private readonly CrispConfiguration _config;

    public AzureDevOpsTools(
        ISourceControlProvider sourceControl,
        IOptions<CrispConfiguration> config)
    {
        _sourceControl = sourceControl;
        _config = config.Value;
    }

    [McpServerTool(Name = "create_repository")]
    [Description("Creates a new Git repository in Azure DevOps Server")]
    public async Task<CreateRepositoryResult> CreateRepositoryAsync(
        [Description("Repository name")] string name,
        [Description("Repository description")] string? description = null)
    {
        var result = await _sourceControl.CreateRepositoryAsync(
            name, description, RepositoryVisibility.Private);

        return new CreateRepositoryResult
        {
            Success = true,
            Name = result.Name,
            Url = result.Url ?? string.Empty,
            CloneUrl = result.CloneUrl ?? string.Empty,
            DefaultBranch = result.DefaultBranch,
            Project = _config.AzureDevOps.Project
        };
    }

    [McpServerTool(Name = "configure_branch_policies")]
    [Description("Configures branch policies on a repository")]
    public async Task<BranchPolicyResult> ConfigureBranchPoliciesAsync(
        [Description("Repository name")] string repositoryName,
        [Description("Branch name to protect")] string branchName = "main")
    {
        await _sourceControl.ConfigureBranchProtectionAsync(repositoryName, branchName);

        return new BranchPolicyResult
        {
            Success = true,
            Message = $"Branch policies configured for {branchName}"
        };
    }

    [McpServerTool(Name = "create_pipeline")]
    [Description("Creates an Azure Pipeline definition")]
    public async Task<PipelineCreationResult> CreatePipelineAsync(
        [Description("Repository name")] string repositoryName,
        [Description("Pipeline YAML content")] string pipelineContent,
        [Description("Path to pipeline YAML file")] string pipelinePath = "azure-pipelines.yml")
    {
        var pipelineUrl = await _sourceControl.CreatePipelineAsync(
            repositoryName, pipelineContent, pipelinePath);

        return new PipelineCreationResult
        {
            Success = true,
            PipelineUrl = pipelineUrl
        };
    }

    [McpServerTool(Name = "trigger_pipeline")]
    [Description("Triggers an Azure Pipeline build")]
    public async Task<PipelineTriggerResult> TriggerPipelineAsync(
        [Description("Repository name")] string repositoryName,
        [Description("Branch name")] string branchName = "main",
        [Description("Pipeline ID")] string? pipelineId = null)
    {
        var (runUrl, runId) = await _sourceControl.TriggerPipelineAsync(
            repositoryName, pipelineId, branchName);

        return new PipelineTriggerResult
        {
            Success = true,
            RunUrl = runUrl,
            RunId = runId
        };
    }

    [McpServerTool(Name = "get_build_status")]
    [Description("Gets the status of an Azure Pipeline build")]
    public async Task<BuildStatusResult> GetBuildStatusAsync(
        [Description("Repository name")] string repositoryName,
        [Description("Build ID")] string buildId)
    {
        var (status, result) = await _sourceControl.GetPipelineStatusAsync(repositoryName, buildId);

        return new BuildStatusResult
        {
            BuildId = buildId,
            Status = status,
            Result = result
        };
    }

    [McpServerTool(Name = "validate_connection")]
    [Description("Validates the Azure DevOps Server connection")]
    public async Task<ConnectionValidationResult> ValidateConnectionAsync()
    {
        var isValid = await _sourceControl.ValidateConnectionAsync();

        return new ConnectionValidationResult
        {
            IsValid = isValid,
            ServerUrl = _config.AzureDevOps.ServerUrl,
            Collection = _config.AzureDevOps.Collection,
            Project = _config.AzureDevOps.Project
        };
    }

    [McpServerTool(Name = "get_server_info")]
    [Description("Gets Azure DevOps Server configuration information")]
    public ServerInfoResult GetServerInfo()
    {
        return new ServerInfoResult
        {
            ServerUrl = _config.AzureDevOps.ServerUrl,
            Collection = _config.AzureDevOps.Collection,
            Project = _config.AzureDevOps.Project,
            PipelineFormat = _config.AzureDevOps.PipelineFormat.ToString(),
            AgentPool = _config.AzureDevOps.AgentPool,
            NuGetFeedUrl = _config.AzureDevOps.NuGetFeedUrl
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
    public string? Project { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record BranchPolicyResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
}

public sealed record PipelineCreationResult
{
    public required bool Success { get; init; }
    public required string PipelineUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record PipelineTriggerResult
{
    public required bool Success { get; init; }
    public required string RunUrl { get; init; }
    public required string RunId { get; init; }
}

public sealed record BuildStatusResult
{
    public required string BuildId { get; init; }
    public required string Status { get; init; }
    public string? Result { get; init; }
}

public sealed record ConnectionValidationResult
{
    public required bool IsValid { get; init; }
    public string? ServerUrl { get; init; }
    public string? Collection { get; init; }
    public string? Project { get; init; }
}

public sealed record ServerInfoResult
{
    public string? ServerUrl { get; init; }
    public string? Collection { get; init; }
    public string? Project { get; init; }
    public string? PipelineFormat { get; init; }
    public string? AgentPool { get; init; }
    public string? NuGetFeedUrl { get; init; }
}
