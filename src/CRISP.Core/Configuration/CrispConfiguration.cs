using CRISP.Core.Enums;

namespace CRISP.Core.Configuration;

/// <summary>
/// Root configuration for a CRISP session.
/// </summary>
public sealed class CrispConfiguration
{
    /// <summary>
    /// The source control platform to use. Defaults to GitHub.
    /// </summary>
    public ScmPlatform ScmPlatform { get; set; } = ScmPlatform.GitHub;

    /// <summary>
    /// GitHub-specific configuration.
    /// </summary>
    public GitHubConfiguration GitHub { get; set; } = new();

    /// <summary>
    /// Azure DevOps Server-specific configuration.
    /// </summary>
    public AzureDevOpsConfiguration AzureDevOps { get; set; } = new();

    /// <summary>
    /// Common configuration shared across platforms.
    /// </summary>
    public CommonConfiguration Common { get; set; } = new();
}

/// <summary>
/// GitHub platform configuration.
/// </summary>
public sealed class GitHubConfiguration
{
    /// <summary>
    /// GitHub owner (user or organization).
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Repository visibility.
    /// </summary>
    public RepositoryVisibility Visibility { get; set; } = RepositoryVisibility.Private;

    /// <summary>
    /// Personal access token for GitHub API.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// GitHub API base URL (for GitHub Enterprise).
    /// </summary>
    public string? ApiBaseUrl { get; set; }
}

/// <summary>
/// Azure DevOps Server configuration.
/// </summary>
public sealed class AzureDevOpsConfiguration
{
    /// <summary>
    /// Azure DevOps Server URL (e.g., https://azuredevops.contoso.local).
    /// </summary>
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Collection name (e.g., DefaultCollection).
    /// </summary>
    public string Collection { get; set; } = "DefaultCollection";

    /// <summary>
    /// Team project name. Leave empty to create a new project.
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// Pipeline format to use.
    /// </summary>
    public PipelineFormat PipelineFormat { get; set; } = PipelineFormat.Yaml;

    /// <summary>
    /// Personal access token for Azure DevOps API.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Build agent pool name.
    /// </summary>
    public string? AgentPool { get; set; }

    /// <summary>
    /// Internal NuGet feed URL.
    /// </summary>
    public string? NuGetFeedUrl { get; set; }
}

/// <summary>
/// Common configuration shared across platforms.
/// </summary>
public sealed class CommonConfiguration
{
    /// <summary>
    /// Default branch name.
    /// </summary>
    public string DefaultBranch { get; set; } = "main";

    /// <summary>
    /// Whether to generate CI/CD pipeline.
    /// </summary>
    public bool GenerateCiCd { get; set; } = true;

    /// <summary>
    /// Optional URL to a versioned template repository.
    /// </summary>
    public string? TemplateRepo { get; set; }

    /// <summary>
    /// Temporary workspace directory for scaffolding.
    /// </summary>
    public string WorkspaceDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "crisp-workspaces");
}
