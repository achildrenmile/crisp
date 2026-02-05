using CRISP.Core.Enums;

namespace CRISP.Core.Models;

/// <summary>
/// Represents the developer's project requirements extracted during intake.
/// </summary>
public sealed class ProjectRequirements
{
    /// <summary>
    /// Project name (e.g., "order-service", "dashboard-frontend").
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Project description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Primary language/runtime.
    /// </summary>
    public required ProjectLanguage Language { get; init; }

    /// <summary>
    /// Runtime version (e.g., ".NET 8", "Node 20", "Python 3.12").
    /// </summary>
    public required string RuntimeVersion { get; init; }

    /// <summary>
    /// Framework to use.
    /// </summary>
    public required ProjectFramework Framework { get; init; }

    /// <summary>
    /// Source control platform.
    /// </summary>
    public ScmPlatform ScmPlatform { get; init; } = ScmPlatform.GitHub;

    /// <summary>
    /// Repository visibility.
    /// </summary>
    public RepositoryVisibility Visibility { get; init; } = RepositoryVisibility.Private;

    /// <summary>
    /// Linting/formatting tools to include.
    /// </summary>
    public IReadOnlyList<string> LintingTools { get; init; } = [];

    /// <summary>
    /// Testing framework to use.
    /// </summary>
    public string? TestingFramework { get; init; }

    /// <summary>
    /// Whether to include container support (Dockerfile, docker-compose).
    /// </summary>
    public bool IncludeContainerSupport { get; init; }

    /// <summary>
    /// Additional tooling (Dependabot, SonarQube, pre-commit hooks, etc.).
    /// </summary>
    public IReadOnlyList<string> AdditionalTooling { get; init; } = [];

    /// <summary>
    /// Custom configuration overrides.
    /// </summary>
    public IReadOnlyDictionary<string, string> CustomConfiguration { get; init; } =
        new Dictionary<string, string>();
}
