using CRISP.Adr;

namespace CRISP.Enterprise;

/// <summary>
/// Everything a module needs to know about the project being scaffolded.
/// Assembled by the agent after intake and updated during execution.
/// </summary>
public sealed record ProjectContext
{
    // ── Identity ──

    /// <summary>
    /// Project name (e.g., "order-service").
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Project description.
    /// </summary>
    public string? ProjectDescription { get; init; }

    // ── Technical Stack ──

    /// <summary>
    /// Programming language: "csharp", "python", "typescript", "java", "dart".
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// Runtime version: ".NET 10", "Python 3.12", "Node 22", "Java 21".
    /// </summary>
    public required string Runtime { get; init; }

    /// <summary>
    /// Framework: "ASP.NET Core Web API", "FastAPI", "Next.js", "Spring Boot", "Shelf".
    /// </summary>
    public required string Framework { get; init; }

    /// <summary>
    /// True if this project exposes HTTP endpoints.
    /// </summary>
    public bool IsApiProject { get; init; }

    /// <summary>
    /// Testing framework: "xunit", "pytest", "jest", "junit".
    /// </summary>
    public string? TestFramework { get; init; }

    /// <summary>
    /// Linter/formatter: "roslyn", "ruff", "eslint", "checkstyle".
    /// </summary>
    public string? Linter { get; init; }

    /// <summary>
    /// Whether Docker support is included.
    /// </summary>
    public bool HasDocker { get; init; }

    /// <summary>
    /// Whether a database is configured.
    /// </summary>
    public bool HasDatabase { get; init; }

    /// <summary>
    /// Database type: "postgresql", "sqlserver", "mongodb", etc.
    /// </summary>
    public string? DatabaseType { get; init; }

    // ── SCM & CI/CD ──

    /// <summary>
    /// SCM platform: "github" or "azure-devops".
    /// </summary>
    public required string ScmPlatform { get; init; }

    /// <summary>
    /// Repository URL.
    /// </summary>
    public required string RepositoryUrl { get; init; }

    /// <summary>
    /// Default branch name.
    /// </summary>
    public required string DefaultBranch { get; init; }

    /// <summary>
    /// CI pipeline file path: ".github/workflows/ci.yml" or "azure-pipelines.yml".
    /// </summary>
    public string? CiPipelineFile { get; init; }

    // ── GitHub-specific ──

    /// <summary>
    /// GitHub owner (username or organization).
    /// </summary>
    public string? GitHubOwner { get; init; }

    /// <summary>
    /// GitHub repository visibility: "public" or "private".
    /// </summary>
    public string? GitHubVisibility { get; init; }

    // ── Azure DevOps-specific ──

    /// <summary>
    /// Azure DevOps Server URL.
    /// </summary>
    public string? AzdoServerUrl { get; init; }

    /// <summary>
    /// Azure DevOps collection name.
    /// </summary>
    public string? AzdoCollection { get; init; }

    /// <summary>
    /// Azure DevOps project name.
    /// </summary>
    public string? AzdoProject { get; init; }

    // ── Ownership ──

    /// <summary>
    /// Owning team name.
    /// </summary>
    public string? TeamName { get; init; }

    /// <summary>
    /// Owning team email.
    /// </summary>
    public string? TeamEmail { get; init; }

    /// <summary>
    /// Code owners (GitHub usernames or Azure DevOps identities).
    /// </summary>
    public List<string> Owners { get; init; } = [];

    /// <summary>
    /// Developer who initiated scaffolding.
    /// </summary>
    public string? RequestedBy { get; init; }

    // ── Organization ──

    /// <summary>
    /// Organization name for documentation.
    /// </summary>
    public string? OrganizationName { get; init; }

    // ── Workspace ──

    /// <summary>
    /// Absolute path to scaffolded repo root.
    /// </summary>
    public required string WorkspacePath { get; init; }

    // ── Files already generated ──

    /// <summary>
    /// Relative paths of all files created so far.
    /// </summary>
    public List<string> GeneratedFiles { get; init; } = [];

    // ── Decision collector (for ADR integration) ──

    /// <summary>
    /// Collector for recording architecture decisions.
    /// </summary>
    public required DecisionCollector DecisionCollector { get; init; }

    // ── Enterprise Configuration ──

    /// <summary>
    /// License SPDX identifier.
    /// </summary>
    public string LicenseSpdx { get; init; } = "UNLICENSED";

    /// <summary>
    /// Branching strategy: "trunk-based", "github-flow", "gitflow".
    /// </summary>
    public string BranchingStrategy { get; init; } = "trunk-based";

    /// <summary>
    /// Security contact email.
    /// </summary>
    public string? SecurityContactEmail { get; init; }

    /// <summary>
    /// Whether to add license headers to source files.
    /// </summary>
    public bool AddLicenseHeaders { get; init; }

    /// <summary>
    /// SBOM format: "cyclonedx" or "spdx".
    /// </summary>
    public string SbomFormat { get; init; } = "cyclonedx";

    /// <summary>
    /// Secrets manager: "none", "azure-keyvault", "aws-secrets", "hashicorp-vault".
    /// </summary>
    public string SecretsManager { get; init; } = "none";

    /// <summary>
    /// Observability provider: "opentelemetry", "applicationinsights", "datadog", "none".
    /// </summary>
    public string ObservabilityProvider { get; init; } = "opentelemetry";

    /// <summary>
    /// Default port for the application.
    /// </summary>
    public int Port { get; init; } = 8080;
}
