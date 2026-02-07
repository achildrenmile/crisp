namespace CRISP.Adr;

/// <summary>
/// Represents a single architectural decision captured during the CRISP scaffolding process.
/// The agent populates this as it makes each decision.
/// </summary>
public sealed record AdrDecision
{
    /// <summary>Sequential ADR number, auto-assigned.</summary>
    public int Number { get; init; }

    /// <summary>Short, descriptive title. Example: "Use FastAPI as web framework"</summary>
    public required string Title { get; init; }

    /// <summary>Current status of this decision.</summary>
    public AdrStatus Status { get; init; } = AdrStatus.Accepted;

    /// <summary>Date the decision was made (defaults to scaffold time).</summary>
    public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Who made the decision. Default: "CRISP Agent (AI-assisted)"</summary>
    public string Deciders { get; init; } = "CRISP Agent (AI-assisted)";

    /// <summary>
    /// The context and problem statement. Why was this decision needed?
    /// Example: "We need a web framework for the Python REST API. The framework should
    /// support async operations, have strong typing support, and auto-generate OpenAPI docs."
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// The decision itself. What was chosen?
    /// Example: "Use FastAPI as the web framework for the REST API."
    /// </summary>
    public required string Decision { get; init; }

    /// <summary>
    /// Brief rationale for the decision.
    /// Example: "FastAPI provides native async support, Pydantic-based validation with
    /// automatic OpenAPI schema generation, and excellent performance benchmarks."
    /// </summary>
    public required string Rationale { get; init; }

    /// <summary>
    /// Alternatives that were considered with brief reasons for rejection.
    /// Key = alternative name, Value = reason not chosen.
    /// Example: { "Flask": "No built-in async, no auto OpenAPI", "Django REST": "Too heavyweight for a microservice" }
    /// </summary>
    public Dictionary<string, string> AlternativesConsidered { get; init; } = new();

    /// <summary>
    /// Positive and negative consequences of this decision.
    /// </summary>
    public List<string> Consequences { get; init; } = [];

    /// <summary>
    /// Category/tag for grouping. Examples: "framework", "testing", "ci-cd", "infrastructure",
    /// "persistence", "authentication", "code-style", "deployment"
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Files or directories in the scaffolded repo that this decision directly affects.
    /// Example: ["src/main.py", "requirements.txt", "Dockerfile"]
    /// </summary>
    public List<string> RelatedFiles { get; init; } = [];

    /// <summary>
    /// References to other ADR numbers that this decision relates to or supersedes.
    /// </summary>
    public List<int> RelatedAdrs { get; init; } = [];

    /// <summary>
    /// If this ADR supersedes another, the number of the superseded ADR.
    /// </summary>
    public int? Supersedes { get; init; }

    /// <summary>
    /// Generates a kebab-case filename slug from the title.
    /// </summary>
    public string GetFileNameSlug()
    {
        var slug = Title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(":", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("(", "")
            .Replace(")", "");

        // Remove consecutive hyphens
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }

    /// <summary>
    /// Gets the full filename for this ADR.
    /// </summary>
    public string GetFileName() => $"{Number:D4}-{GetFileNameSlug()}.md";
}
