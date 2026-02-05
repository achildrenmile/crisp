using CRISP.Core.Interfaces;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;

namespace CRISP.Git;

/// <summary>
/// Implementation of local Git operations using LibGit2Sharp.
/// </summary>
public sealed class GitOperations : IGitOperations
{
    private readonly ILogger<GitOperations> _logger;

    public GitOperations(ILogger<GitOperations> logger)
    {
        _logger = logger;
    }

    public Task InitializeRepositoryAsync(
        string path,
        string defaultBranch = "main",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Git repository at {Path} with default branch {Branch}", path, defaultBranch);

        Repository.Init(path, isBare: false);

        // Set the default branch name
        using var repo = new Repository(path);

        // Create an initial commit to establish the branch
        // LibGit2Sharp requires at least one commit to rename the branch
        var signature = new Signature("CRISP", "crisp@scaffold.local", DateTimeOffset.UtcNow);

        // Create .gitkeep to have something to commit for initial setup
        var gitkeepPath = Path.Combine(path, ".gitkeep");
        File.WriteAllText(gitkeepPath, "");

        Commands.Stage(repo, ".gitkeep");

        repo.Commit("Initial commit", signature, signature, new CommitOptions { AllowEmptyCommit = false });

        // Rename master to the desired default branch if different
        if (repo.Head.FriendlyName != defaultBranch)
        {
            var branch = repo.Branches[repo.Head.FriendlyName];
            repo.Branches.Rename(branch, defaultBranch);
        }

        // Remove the temporary .gitkeep file from disk (will be replaced by actual files)
        File.Delete(gitkeepPath);

        _logger.LogInformation("Git repository initialized successfully");
        return Task.CompletedTask;
    }

    public Task StageAllAsync(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Staging all files in {Path}", path);

        using var repo = new Repository(path);
        Commands.Stage(repo, "*");

        _logger.LogInformation("All files staged");
        return Task.CompletedTask;
    }

    public Task<string> CommitAsync(
        string path,
        string message,
        string authorName,
        string authorEmail,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating commit in {Path}", path);

        using var repo = new Repository(path);
        var signature = new Signature(authorName, authorEmail, DateTimeOffset.UtcNow);
        var commit = repo.Commit(message, signature, signature);

        _logger.LogInformation("Commit created: {Sha}", commit.Sha);
        return Task.FromResult(commit.Sha);
    }

    public Task AddRemoteAsync(
        string path,
        string remoteName,
        string remoteUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding remote {RemoteName} -> {RemoteUrl} in {Path}", remoteName, remoteUrl, path);

        using var repo = new Repository(path);

        // Remove existing remote if it exists
        var existingRemote = repo.Network.Remotes[remoteName];
        if (existingRemote != null)
        {
            repo.Network.Remotes.Remove(remoteName);
        }

        repo.Network.Remotes.Add(remoteName, remoteUrl);

        _logger.LogInformation("Remote added successfully");
        return Task.CompletedTask;
    }

    public Task PushAsync(
        string path,
        string remoteName,
        string branchName,
        GitCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pushing {Branch} to {Remote} from {Path}", branchName, remoteName, path);

        using var repo = new Repository(path);
        var remote = repo.Network.Remotes[remoteName]
            ?? throw new InvalidOperationException($"Remote '{remoteName}' not found");

        var pushOptions = new PushOptions
        {
            CredentialsProvider = CreateCredentialsProvider(credentials)
        };

        var refSpec = $"refs/heads/{branchName}:refs/heads/{branchName}";
        repo.Network.Push(remote, refSpec, pushOptions);

        _logger.LogInformation("Push completed successfully");
        return Task.CompletedTask;
    }

    public Task<string> AmendCommitAsync(
        string path,
        string message,
        string authorName,
        string authorEmail,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Amending commit in {Path}", path);

        using var repo = new Repository(path);
        var signature = new Signature(authorName, authorEmail, DateTimeOffset.UtcNow);

        // Stage any new changes
        Commands.Stage(repo, "*");

        var commit = repo.Commit(message, signature, signature, new CommitOptions { AmendPreviousCommit = true });

        _logger.LogInformation("Commit amended: {Sha}", commit.Sha);
        return Task.FromResult(commit.Sha);
    }

    public Task ForcePushAsync(
        string path,
        string remoteName,
        string branchName,
        GitCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Force pushing {Branch} to {Remote} from {Path}", branchName, remoteName, path);

        using var repo = new Repository(path);
        var remote = repo.Network.Remotes[remoteName]
            ?? throw new InvalidOperationException($"Remote '{remoteName}' not found");

        var pushOptions = new PushOptions
        {
            CredentialsProvider = CreateCredentialsProvider(credentials)
        };

        var refSpec = $"+refs/heads/{branchName}:refs/heads/{branchName}";
        repo.Network.Push(remote, refSpec, pushOptions);

        _logger.LogInformation("Force push completed successfully");
        return Task.CompletedTask;
    }

    public string GetCurrentBranch(string path)
    {
        using var repo = new Repository(path);
        return repo.Head.FriendlyName;
    }

    public string GetLatestCommitSha(string path)
    {
        using var repo = new Repository(path);
        return repo.Head.Tip.Sha;
    }

    private static CredentialsHandler CreateCredentialsProvider(GitCredentials credentials)
    {
        return (_, _, _) => new UsernamePasswordCredentials
        {
            Username = credentials.Username,
            Password = credentials.Password
        };
    }
}
