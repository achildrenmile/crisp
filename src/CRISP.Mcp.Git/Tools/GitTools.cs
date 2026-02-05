using System.ComponentModel;
using CRISP.Core.Interfaces;
using ModelContextProtocol.Server;

namespace CRISP.Mcp.Git.Tools;

/// <summary>
/// MCP tools for local Git operations.
/// </summary>
[McpServerToolType]
public sealed class GitTools
{
    private readonly IGitOperations _git;

    public GitTools(IGitOperations git)
    {
        _git = git;
    }

    [McpServerTool(Name = "init")]
    [Description("Initializes a new Git repository")]
    public async Task<GitResult> InitAsync(
        [Description("Repository path")] string path,
        [Description("Default branch name")] string defaultBranch = "main")
    {
        await _git.InitializeRepositoryAsync(path, defaultBranch);
        return new GitResult { Success = true, Message = $"Repository initialized at {path}" };
    }

    [McpServerTool(Name = "stage_all")]
    [Description("Stages all files in the repository")]
    public async Task<GitResult> StageAllAsync(
        [Description("Repository path")] string path)
    {
        await _git.StageAllAsync(path);
        return new GitResult { Success = true, Message = "All files staged" };
    }

    [McpServerTool(Name = "commit")]
    [Description("Creates a commit with staged changes")]
    public async Task<CommitResult> CommitAsync(
        [Description("Repository path")] string path,
        [Description("Commit message")] string message,
        [Description("Author name")] string authorName = "CRISP Agent",
        [Description("Author email")] string authorEmail = "crisp@scaffold.local")
    {
        var sha = await _git.CommitAsync(path, message, authorName, authorEmail);
        return new CommitResult { Success = true, Sha = sha, Message = "Commit created" };
    }

    [McpServerTool(Name = "add_remote")]
    [Description("Adds a remote to the repository")]
    public async Task<GitResult> AddRemoteAsync(
        [Description("Repository path")] string path,
        [Description("Remote name")] string remoteName,
        [Description("Remote URL")] string remoteUrl)
    {
        await _git.AddRemoteAsync(path, remoteName, remoteUrl);
        return new GitResult { Success = true, Message = $"Remote '{remoteName}' added" };
    }

    [McpServerTool(Name = "push")]
    [Description("Pushes changes to the remote repository")]
    public async Task<GitResult> PushAsync(
        [Description("Repository path")] string path,
        [Description("Remote name")] string remoteName,
        [Description("Branch name")] string branchName,
        [Description("Git username")] string username,
        [Description("Git password/token")] string password)
    {
        var credentials = new GitCredentials { Username = username, Password = password };
        await _git.PushAsync(path, remoteName, branchName, credentials);
        return new GitResult { Success = true, Message = $"Pushed to {remoteName}/{branchName}" };
    }

    [McpServerTool(Name = "get_current_branch")]
    [Description("Gets the current branch name")]
    public BranchResult GetCurrentBranch(
        [Description("Repository path")] string path)
    {
        var branch = _git.GetCurrentBranch(path);
        return new BranchResult { Branch = branch };
    }

    [McpServerTool(Name = "get_latest_commit")]
    [Description("Gets the latest commit SHA")]
    public CommitInfoResult GetLatestCommit(
        [Description("Repository path")] string path)
    {
        var sha = _git.GetLatestCommitSha(path);
        return new CommitInfoResult { Sha = sha };
    }
}

public sealed record GitResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
}

public sealed record CommitResult
{
    public required bool Success { get; init; }
    public required string Sha { get; init; }
    public required string Message { get; init; }
}

public sealed record BranchResult
{
    public required string Branch { get; init; }
}

public sealed record CommitInfoResult
{
    public required string Sha { get; init; }
}
