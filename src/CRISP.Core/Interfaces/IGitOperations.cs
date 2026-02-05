namespace CRISP.Core.Interfaces;

/// <summary>
/// Local Git operations using LibGit2Sharp.
/// </summary>
public interface IGitOperations
{
    /// <summary>
    /// Initializes a new Git repository.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <param name="defaultBranch">Default branch name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeRepositoryAsync(
        string path,
        string defaultBranch = "main",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages all files in the repository.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StageAllAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a commit with the staged changes.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <param name="message">Commit message.</param>
    /// <param name="authorName">Author name.</param>
    /// <param name="authorEmail">Author email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Commit SHA.</returns>
    Task<string> CommitAsync(
        string path,
        string message,
        string authorName,
        string authorEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a remote to the repository.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <param name="remoteName">Remote name (typically "origin").</param>
    /// <param name="remoteUrl">Remote URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddRemoteAsync(
        string path,
        string remoteName,
        string remoteUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes changes to the remote repository.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <param name="remoteName">Remote name.</param>
    /// <param name="branchName">Branch name.</param>
    /// <param name="credentials">Credentials for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PushAsync(
        string path,
        string remoteName,
        string branchName,
        GitCredentials credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Amends the last commit with new changes.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <param name="message">New commit message.</param>
    /// <param name="authorName">Author name.</param>
    /// <param name="authorEmail">Author email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New commit SHA.</returns>
    Task<string> AmendCommitAsync(
        string path,
        string message,
        string authorName,
        string authorEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Force pushes to the remote repository.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <param name="remoteName">Remote name.</param>
    /// <param name="branchName">Branch name.</param>
    /// <param name="credentials">Credentials for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ForcePushAsync(
        string path,
        string remoteName,
        string branchName,
        GitCredentials credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current branch name.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <returns>Current branch name.</returns>
    string GetCurrentBranch(string path);

    /// <summary>
    /// Gets the latest commit SHA.
    /// </summary>
    /// <param name="path">Repository path.</param>
    /// <returns>Commit SHA.</returns>
    string GetLatestCommitSha(string path);
}

/// <summary>
/// Git credentials for authentication.
/// </summary>
public sealed class GitCredentials
{
    /// <summary>
    /// Username for authentication.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Password or personal access token.
    /// </summary>
    public required string Password { get; init; }
}
