using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace CRISP.Mcp.GitHub;

/// <summary>
/// GitHub implementation of source control provider.
/// </summary>
public sealed class GitHubSourceControlProvider : ISourceControlProvider
{
    private readonly ILogger<GitHubSourceControlProvider> _logger;
    private readonly GitHubClient _client;
    private readonly GitHubConfiguration _config;

    public GitHubSourceControlProvider(
        ILogger<GitHubSourceControlProvider> logger,
        IOptions<CrispConfiguration> config)
    {
        _logger = logger;
        _config = config.Value.GitHub;

        _client = new GitHubClient(new ProductHeaderValue("CRISP-Agent"));

        if (!string.IsNullOrEmpty(_config.Token))
        {
            _client.Credentials = new Credentials(_config.Token);
        }

        if (!string.IsNullOrEmpty(_config.ApiBaseUrl))
        {
            _client = new GitHubClient(new ProductHeaderValue("CRISP-Agent"), new Uri(_config.ApiBaseUrl));
            if (!string.IsNullOrEmpty(_config.Token))
            {
                _client.Credentials = new Credentials(_config.Token);
            }
        }
    }

    public ScmPlatform Platform => ScmPlatform.GitHub;

    public async Task<RepositoryDetails> CreateRepositoryAsync(
        string name,
        string? description,
        RepositoryVisibility visibility,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating GitHub repository: {Owner}/{Name}", _config.Owner, name);

        var newRepo = new NewRepository(name)
        {
            Description = description,
            Private = visibility != RepositoryVisibility.Public,
            AutoInit = false,
            HasIssues = true,
            HasProjects = true,
            HasWiki = false
        };

        Repository repo;

        // Check if owner is an organization or user
        try
        {
            var org = await _client.Organization.Get(_config.Owner);
            repo = await _client.Repository.Create(_config.Owner, newRepo);
        }
        catch (NotFoundException)
        {
            // Owner is a user, not an organization
            repo = await _client.Repository.Create(newRepo);
        }

        _logger.LogInformation("Repository created: {Url}", repo.HtmlUrl);

        return new RepositoryDetails
        {
            Name = repo.Name,
            Url = repo.HtmlUrl,
            CloneUrl = repo.CloneUrl,
            Owner = _config.Owner,
            Visibility = visibility.ToString().ToLowerInvariant(),
            DefaultBranch = repo.DefaultBranch ?? "main"
        };
    }

    public async Task ConfigureBranchProtectionAsync(
        string repositoryName,
        string branchName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring branch protection for {Owner}/{Repo}:{Branch}",
            _config.Owner, repositoryName, branchName);

        try
        {
            var protection = new BranchProtectionSettingsUpdate(
                requiredStatusChecks: new BranchProtectionRequiredStatusChecksUpdate(true, ["ci"]),
                requiredPullRequestReviews: new BranchProtectionPullRequestReviewsUpdate(
                    dismissStaleReviews: true,
                    requireCodeOwnerReviews: false,
                    requiredApprovingReviewCount: 1
                ),
                restrictions: null,
                enforceAdmins: false
            );

            await _client.Repository.Branch.UpdateBranchProtection(
                _config.Owner, repositoryName, branchName, protection);

            _logger.LogInformation("Branch protection configured successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to configure branch protection (may require higher permissions)");
        }
    }

    public async Task<string> CreatePipelineAsync(
        string repositoryName,
        string pipelineContent,
        string pipelinePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating workflow file at {Path} in {Owner}/{Repo}",
            pipelinePath, _config.Owner, repositoryName);

        // The workflow file should be committed with the initial push
        // This method returns the expected workflow URL
        var workflowUrl = $"https://github.com/{_config.Owner}/{repositoryName}/actions";

        return workflowUrl;
    }

    public async Task<(string RunUrl, string RunId)> TriggerPipelineAsync(
        string repositoryName,
        string? pipelineId,
        string branchName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Triggering workflow in {Owner}/{Repo} on branch {Branch}",
            _config.Owner, repositoryName, branchName);

        // GitHub Actions are triggered automatically on push
        // We'll get the latest workflow run
        await Task.Delay(5000, cancellationToken); // Wait for workflow to start

        var runs = await _client.Actions.Workflows.Runs.List(_config.Owner, repositoryName);
        var latestRun = runs.WorkflowRuns.FirstOrDefault();

        if (latestRun != null)
        {
            _logger.LogInformation("Found workflow run: {RunId}", latestRun.Id);
            return (latestRun.HtmlUrl, latestRun.Id.ToString());
        }

        var runUrl = $"https://github.com/{_config.Owner}/{repositoryName}/actions";
        return (runUrl, "pending");
    }

    public async Task<(string Status, string? Conclusion)> GetPipelineStatusAsync(
        string repositoryName,
        string runId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting workflow status for run {RunId} in {Owner}/{Repo}",
            runId, _config.Owner, repositoryName);

        if (!long.TryParse(runId, out var runIdLong))
        {
            return ("unknown", null);
        }

        var run = await _client.Actions.Workflows.Runs.Get(_config.Owner, repositoryName, runIdLong);

        return (run.Status.Value.ToString(), run.Conclusion?.Value.ToString());
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.User.Current();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate GitHub connection");
            return false;
        }
    }

    public async Task<string> GetAuthenticatedUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await _client.User.Current();
        return user.Login;
    }
}
