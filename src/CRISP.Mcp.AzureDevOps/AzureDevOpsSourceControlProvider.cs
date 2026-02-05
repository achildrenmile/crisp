using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRISP.Mcp.AzureDevOps;

/// <summary>
/// Azure DevOps Server implementation of source control provider.
/// </summary>
public sealed class AzureDevOpsSourceControlProvider : ISourceControlProvider, IDisposable
{
    private readonly ILogger<AzureDevOpsSourceControlProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly AzureDevOpsConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureDevOpsSourceControlProvider(
        ILogger<AzureDevOpsSourceControlProvider> logger,
        IOptions<CrispConfiguration> config,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config.Value.AzureDevOps;
        _httpClient = httpClientFactory.CreateClient("AzureDevOps");

        // Configure authentication
        if (!string.IsNullOrEmpty(_config.Token))
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_config.Token}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }

        _httpClient.BaseAddress = new Uri($"{_config.ServerUrl}/{_config.Collection}/");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public ScmPlatform Platform => ScmPlatform.AzureDevOps;

    public async Task<RepositoryDetails> CreateRepositoryAsync(
        string name,
        string? description,
        RepositoryVisibility visibility,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating Azure DevOps repository: {Project}/{Name}",
            _config.Project, name);

        var projectId = await GetOrCreateProjectAsync(cancellationToken);

        var createRepoRequest = new
        {
            name,
            project = new { id = projectId }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.Project}/_apis/git/repositories?api-version=7.0",
            createRepoRequest,
            _jsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var repo = await response.Content.ReadFromJsonAsync<AzureDevOpsRepository>(
            _jsonOptions, cancellationToken);

        _logger.LogInformation("Repository created: {Url}", repo?.WebUrl);

        return new RepositoryDetails
        {
            Name = repo?.Name ?? name,
            Url = repo?.WebUrl,
            CloneUrl = repo?.RemoteUrl,
            Owner = _config.Project ?? "DefaultCollection",
            Visibility = visibility.ToString().ToLowerInvariant(),
            DefaultBranch = repo?.DefaultBranch ?? "refs/heads/main"
        };
    }

    public async Task ConfigureBranchProtectionAsync(
        string repositoryName,
        string branchName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring branch policies for {Project}/{Repo}:{Branch}",
            _config.Project, repositoryName, branchName);

        try
        {
            // Get repository ID
            var repoResponse = await _httpClient.GetAsync(
                $"{_config.Project}/_apis/git/repositories/{repositoryName}?api-version=7.0",
                cancellationToken);
            repoResponse.EnsureSuccessStatusCode();

            var repo = await repoResponse.Content.ReadFromJsonAsync<AzureDevOpsRepository>(
                _jsonOptions, cancellationToken);

            if (repo == null)
            {
                throw new InvalidOperationException($"Repository {repositoryName} not found");
            }

            // Create minimum reviewer policy
            var policyRequest = new
            {
                isEnabled = true,
                isBlocking = true,
                type = new { id = "fa4e907d-c16b-4a4c-9dfa-4906e5d171dd" }, // Minimum reviewer count
                settings = new
                {
                    minimumApproverCount = 1,
                    creatorVoteCounts = false,
                    scope = new[]
                    {
                        new
                        {
                            repositoryId = repo.Id,
                            refName = $"refs/heads/{branchName}",
                            matchKind = "Exact"
                        }
                    }
                }
            };

            var policyResponse = await _httpClient.PostAsJsonAsync(
                $"{_config.Project}/_apis/policy/configurations?api-version=7.0",
                policyRequest,
                _jsonOptions,
                cancellationToken);

            if (policyResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Branch policies configured successfully");
            }
            else
            {
                _logger.LogWarning("Failed to configure branch policies: {Status}",
                    policyResponse.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to configure branch policies");
        }
    }

    public async Task<string> CreatePipelineAsync(
        string repositoryName,
        string pipelineContent,
        string pipelinePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating pipeline for {Project}/{Repo}",
            _config.Project, repositoryName);

        // For YAML pipelines, the definition is created from the YAML file in the repo
        // We need to create a build definition that references the YAML file

        var repoResponse = await _httpClient.GetAsync(
            $"{_config.Project}/_apis/git/repositories/{repositoryName}?api-version=7.0",
            cancellationToken);
        repoResponse.EnsureSuccessStatusCode();

        var repo = await repoResponse.Content.ReadFromJsonAsync<AzureDevOpsRepository>(
            _jsonOptions, cancellationToken);

        var pipelineRequest = new
        {
            name = $"{repositoryName}-CI",
            folder = "\\",
            configuration = new
            {
                type = "yaml",
                path = pipelinePath,
                repository = new
                {
                    id = repo?.Id,
                    type = "azureReposGit",
                    name = repositoryName
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.Project}/_apis/pipelines?api-version=7.0",
            pipelineRequest,
            _jsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to create pipeline definition: {Error}", error);
            return $"{_config.ServerUrl}/{_config.Collection}/{_config.Project}/_build";
        }

        var pipeline = await response.Content.ReadFromJsonAsync<AzureDevOpsPipeline>(
            _jsonOptions, cancellationToken);

        var pipelineUrl = $"{_config.ServerUrl}/{_config.Collection}/{_config.Project}/_build?definitionId={pipeline?.Id}";
        _logger.LogInformation("Pipeline created: {Url}", pipelineUrl);

        return pipelineUrl;
    }

    public async Task<(string RunUrl, string RunId)> TriggerPipelineAsync(
        string repositoryName,
        string? pipelineId,
        string branchName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Triggering pipeline for {Project}/{Repo} on branch {Branch}",
            _config.Project, repositoryName, branchName);

        // Get pipeline definition
        var definitionsResponse = await _httpClient.GetAsync(
            $"{_config.Project}/_apis/pipelines?api-version=7.0",
            cancellationToken);

        if (!definitionsResponse.IsSuccessStatusCode)
        {
            return ($"{_config.ServerUrl}/{_config.Collection}/{_config.Project}/_build", "pending");
        }

        var definitions = await definitionsResponse.Content.ReadFromJsonAsync<AzureDevOpsPipelineList>(
            _jsonOptions, cancellationToken);

        var pipeline = definitions?.Value?.FirstOrDefault(p =>
            p.Name?.Contains(repositoryName, StringComparison.OrdinalIgnoreCase) == true);

        if (pipeline == null)
        {
            return ($"{_config.ServerUrl}/{_config.Collection}/{_config.Project}/_build", "pending");
        }

        // Queue a new build
        var runRequest = new
        {
            resources = new
            {
                repositories = new
                {
                    self = new
                    {
                        refName = $"refs/heads/{branchName}"
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.Project}/_apis/pipelines/{pipeline.Id}/runs?api-version=7.0",
            runRequest,
            _jsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ($"{_config.ServerUrl}/{_config.Collection}/{_config.Project}/_build?definitionId={pipeline.Id}", "pending");
        }

        var run = await response.Content.ReadFromJsonAsync<AzureDevOpsPipelineRun>(
            _jsonOptions, cancellationToken);

        var runUrl = $"{_config.ServerUrl}/{_config.Collection}/{_config.Project}/_build/results?buildId={run?.Id}";
        return (runUrl, run?.Id.ToString() ?? "pending");
    }

    public async Task<(string Status, string? Conclusion)> GetPipelineStatusAsync(
        string repositoryName,
        string runId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting build status for run {RunId}", runId);

        var response = await _httpClient.GetAsync(
            $"{_config.Project}/_apis/build/builds/{runId}?api-version=7.0",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return ("unknown", null);
        }

        var build = await response.Content.ReadFromJsonAsync<AzureDevOpsBuild>(
            _jsonOptions, cancellationToken);

        return (build?.Status ?? "unknown", build?.Result);
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "_apis/projects?api-version=7.0",
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Azure DevOps connection");
            return false;
        }
    }

    public async Task<string> GetAuthenticatedUserAsync(CancellationToken cancellationToken = default)
    {
        // Azure DevOps Server doesn't have a direct "current user" API
        // Return the configured project/collection info instead
        return $"{_config.Collection}/{_config.Project}";
    }

    private async Task<string> GetOrCreateProjectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_config.Project))
        {
            throw new InvalidOperationException("Azure DevOps project must be specified");
        }

        var response = await _httpClient.GetAsync(
            $"_apis/projects/{_config.Project}?api-version=7.0",
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var project = await response.Content.ReadFromJsonAsync<AzureDevOpsProject>(
                _jsonOptions, cancellationToken);
            return project?.Id ?? throw new InvalidOperationException("Failed to get project ID");
        }

        throw new InvalidOperationException($"Project '{_config.Project}' not found");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

// DTOs for Azure DevOps API responses
internal sealed class AzureDevOpsRepository
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? WebUrl { get; set; }
    public string? RemoteUrl { get; set; }
    public string? DefaultBranch { get; set; }
}

internal sealed class AzureDevOpsProject
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

internal sealed class AzureDevOpsPipeline
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

internal sealed class AzureDevOpsPipelineList
{
    public List<AzureDevOpsPipeline>? Value { get; set; }
}

internal sealed class AzureDevOpsPipelineRun
{
    public int Id { get; set; }
    public string? State { get; set; }
    public string? Result { get; set; }
}

internal sealed class AzureDevOpsBuild
{
    public int Id { get; set; }
    public string? Status { get; set; }
    public string? Result { get; set; }
}
