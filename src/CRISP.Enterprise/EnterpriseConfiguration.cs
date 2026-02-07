namespace CRISP.Enterprise;

/// <summary>
/// Configuration for all enterprise modules.
/// </summary>
public sealed class EnterpriseConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Enterprise";

    /// <summary>
    /// Module IDs to disable. Empty = all enabled.
    /// </summary>
    public HashSet<string> DisabledModules { get; set; } = [];

    /// <summary>
    /// Organization name used across modules (SECURITY.md, LICENSE, etc.).
    /// </summary>
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Default license SPDX identifier. Example: "MIT", "Apache-2.0", "UNLICENSED".
    /// </summary>
    public string DefaultLicenseSpdx { get; set; } = "UNLICENSED";

    /// <summary>
    /// Default branching strategy: "trunk-based", "github-flow", "gitflow".
    /// </summary>
    public string DefaultBranchingStrategy { get; set; } = "trunk-based";

    /// <summary>
    /// Default security contact email for SECURITY.md.
    /// </summary>
    public string? SecurityContactEmail { get; set; }

    /// <summary>
    /// Whether to add license headers to source files.
    /// </summary>
    public bool AddLicenseHeaders { get; set; }

    /// <summary>
    /// SBOM format: "cyclonedx" or "spdx".
    /// </summary>
    public string SbomFormat { get; set; } = "cyclonedx";

    /// <summary>
    /// Secrets manager integration: "none", "azure-keyvault", "aws-secrets", "hashicorp-vault".
    /// </summary>
    public string SecretsManager { get; set; } = "none";

    /// <summary>
    /// Observability provider: "opentelemetry", "applicationinsights", "datadog", "none".
    /// </summary>
    public string ObservabilityProvider { get; set; } = "opentelemetry";

    /// <summary>
    /// Minimum number of reviewers for PRs.
    /// </summary>
    public int MinReviewers { get; set; } = 1;

    /// <summary>
    /// Whether to dismiss stale reviews on new commits.
    /// </summary>
    public bool DismissStaleReviews { get; set; } = true;

    /// <summary>
    /// Whether to require status checks to pass before merging.
    /// </summary>
    public bool RequireStatusChecks { get; set; } = true;
}
