using CRISP.Core.Enums;
using CRISP.Core.Models;

namespace CRISP.Core.Interfaces;

/// <summary>
/// Abstraction for source control operations. Implemented by platform-specific providers.
/// </summary>
public interface ISourceControlProvider
{
    /// <summary>
    /// Gets the platform this provider supports.
    /// </summary>
    ScmPlatform Platform { get; }

    /// <summary>
    /// Creates a new repository.
    /// </summary>
    /// <param name="name">Repository name.</param>
    /// <param name="description">Repository description.</param>
    /// <param name="visibility">Repository visibility.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Repository details including URLs.</returns>
    Task<RepositoryDetails> CreateRepositoryAsync(
        string name,
        string? description,
        RepositoryVisibility visibility,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures branch protection rules on the default branch.
    /// </summary>
    /// <param name="repositoryName">Repository name.</param>
    /// <param name="branchName">Branch name to protect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConfigureBranchProtectionAsync(
        string repositoryName,
        string branchName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a pipeline/workflow.
    /// </summary>
    /// <param name="repositoryName">Repository name.</param>
    /// <param name="pipelineContent">Pipeline definition content.</param>
    /// <param name="pipelinePath">Path to the pipeline file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pipeline URL after creation.</returns>
    Task<string> CreatePipelineAsync(
        string repositoryName,
        string pipelineContent,
        string pipelinePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers a pipeline/workflow run.
    /// </summary>
    /// <param name="repositoryName">Repository name.</param>
    /// <param name="pipelineId">Pipeline identifier.</param>
    /// <param name="branchName">Branch to run against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Run URL and ID.</returns>
    Task<(string RunUrl, string RunId)> TriggerPipelineAsync(
        string repositoryName,
        string? pipelineId,
        string branchName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a pipeline/workflow run.
    /// </summary>
    /// <param name="repositoryName">Repository name.</param>
    /// <param name="runId">Run identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current status and conclusion.</returns>
    Task<(string Status, string? Conclusion)> GetPipelineStatusAsync(
        string repositoryName,
        string runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the provider is properly configured and can connect.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is successful.</returns>
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the authenticated user or service account name.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User or service account name.</returns>
    Task<string> GetAuthenticatedUserAsync(CancellationToken cancellationToken = default);
}
