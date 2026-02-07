namespace CRISP.Adr;

/// <summary>
/// Collects architectural decisions during the CRISP scaffolding process.
/// The agent calls Record() whenever it makes a significant choice.
/// At the end of scaffolding, the collected decisions are passed to IAdrGenerator.
/// </summary>
public sealed class DecisionCollector
{
    private readonly List<AdrDecision> _decisions = [];
    private readonly object _lock = new();
    private int _nextNumber = 1; // 0 is reserved for meta-ADR
    private string _defaultDeciders = "CRISP Agent (AI-assisted)";

    /// <summary>
    /// Sets the default deciders string for all recorded decisions.
    /// </summary>
    public void SetDeciders(string deciders)
    {
        _defaultDeciders = deciders;
    }

    /// <summary>
    /// Record a decision. Called by the agent during scaffolding.
    /// </summary>
    public void Record(
        string title,
        string context,
        string decision,
        string rationale,
        string category,
        Dictionary<string, string>? alternatives = null,
        List<string>? consequences = null,
        List<string>? relatedFiles = null)
    {
        lock (_lock)
        {
            _decisions.Add(new AdrDecision
            {
                Number = _nextNumber++,
                Title = title,
                Context = context,
                Decision = decision,
                Rationale = rationale,
                Category = ValidateCategory(category),
                Deciders = _defaultDeciders,
                AlternativesConsidered = alternatives ?? [],
                Consequences = consequences ?? [],
                RelatedFiles = relatedFiles ?? []
            });
        }
    }

    /// <summary>
    /// Record a decision with full control over all properties.
    /// </summary>
    public void Record(AdrDecision decision)
    {
        lock (_lock)
        {
            // Assign the next number if not already set
            var adr = decision with
            {
                Number = decision.Number > 0 ? decision.Number : _nextNumber++,
                Deciders = string.IsNullOrEmpty(decision.Deciders) ? _defaultDeciders : decision.Deciders
            };
            _decisions.Add(adr);
        }
    }

    /// <summary>
    /// Gets all collected decisions.
    /// </summary>
    public IReadOnlyList<AdrDecision> GetDecisions()
    {
        lock (_lock)
        {
            return _decisions.OrderBy(d => d.Number).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the count of collected decisions.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _decisions.Count;
            }
        }
    }

    /// <summary>
    /// Clears all collected decisions and resets the counter.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _decisions.Clear();
            _nextNumber = 1;
        }
    }

    private static string ValidateCategory(string category)
    {
        // Normalize to lowercase
        var normalized = category.ToLowerInvariant().Trim();

        // Check if it's a valid category
        if (AdrCategory.All.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            return normalized;
        }

        // Default to "other" for unknown categories
        return AdrCategory.Other;
    }
}
