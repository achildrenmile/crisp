namespace CRISP.Adr;

/// <summary>
/// Categories for grouping and classifying architecture decisions.
/// </summary>
public static class AdrCategory
{
    public const string Framework = "framework";
    public const string Language = "language";
    public const string Testing = "testing";
    public const string CiCd = "ci-cd";
    public const string Infrastructure = "infrastructure";
    public const string Persistence = "persistence";
    public const string Authentication = "authentication";
    public const string CodeStyle = "code-style";
    public const string Deployment = "deployment";
    public const string Monitoring = "monitoring";
    public const string Documentation = "documentation";
    public const string Process = "process";
    public const string Security = "security";
    public const string Compliance = "compliance";
    public const string CodeOwnership = "code-ownership";
    public const string Interfaces = "interfaces";
    public const string Operations = "operations";
    public const string Development = "development";
    public const string Other = "other";

    /// <summary>
    /// All valid category values.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        Framework,
        Language,
        Testing,
        CiCd,
        Infrastructure,
        Persistence,
        Authentication,
        CodeStyle,
        Deployment,
        Monitoring,
        Documentation,
        Process,
        Security,
        Compliance,
        CodeOwnership,
        Interfaces,
        Operations,
        Development,
        Other
    ];

    /// <summary>
    /// Categories considered minor (use short-form template).
    /// </summary>
    public static readonly IReadOnlyList<string> MinorCategories =
    [
        CodeStyle,
        Documentation
    ];

    /// <summary>
    /// Determines if a category should use the short-form ADR template.
    /// </summary>
    public static bool IsMinor(string category) =>
        MinorCategories.Contains(category, StringComparer.OrdinalIgnoreCase);
}
