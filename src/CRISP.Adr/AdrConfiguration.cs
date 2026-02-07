namespace CRISP.Adr;

/// <summary>
/// Configuration options for the ADR (Architecture Decision Records) module.
/// </summary>
public sealed class AdrConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Adr";

    /// <summary>
    /// Output directory relative to repo root. Default: "docs/adr"
    /// </summary>
    public string OutputDirectory { get; set; } = "docs/adr";

    /// <summary>
    /// Template format. Currently only "madr" is supported.
    /// </summary>
    public string TemplateFormat { get; set; } = "madr";

    /// <summary>
    /// Whether to generate an index file (README.md in ADR directory).
    /// </summary>
    public bool GenerateIndex { get; set; } = true;

    /// <summary>
    /// Whether to include a blank template for future manual ADRs.
    /// </summary>
    public bool IncludeTemplate { get; set; } = true;

    /// <summary>
    /// Minimum decision significance to generate a full ADR.
    /// "minor" = short-form, "major" = full MADR template.
    /// </summary>
    public string MinimumSignificance { get; set; } = "minor";

    /// <summary>
    /// Whether to generate the meta-ADR "Use ADRs for architecture decisions".
    /// </summary>
    public bool GenerateMetaAdr { get; set; } = true;

    /// <summary>
    /// Custom organization name for the deciders field. Null = use default.
    /// </summary>
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Gets the deciders string, using organization name if configured.
    /// </summary>
    public string GetDecidersString() =>
        string.IsNullOrWhiteSpace(OrganizationName)
            ? "CRISP Agent (AI-assisted)"
            : $"CRISP Agent ({OrganizationName})";
}
