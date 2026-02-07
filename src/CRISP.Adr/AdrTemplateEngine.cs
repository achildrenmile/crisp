using System.Text;

namespace CRISP.Adr;

/// <summary>
/// Renders ADR decisions to MADR-format markdown.
/// </summary>
public sealed class AdrTemplateEngine
{
    /// <summary>
    /// Renders an ADR decision to full MADR-format markdown.
    /// </summary>
    public string RenderFull(AdrDecision decision)
    {
        var sb = new StringBuilder();

        // Title
        sb.AppendLine($"# {decision.Number}. {decision.Title}");
        sb.AppendLine();

        // Metadata
        sb.AppendLine($"**Date:** {decision.Date:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {FormatStatus(decision)}");
        sb.AppendLine();
        sb.AppendLine($"**Deciders:** {decision.Deciders}");
        sb.AppendLine();

        // Context
        sb.AppendLine("## Context and Problem Statement");
        sb.AppendLine();
        sb.AppendLine(decision.Context);
        sb.AppendLine();

        // Decision
        sb.AppendLine("## Decision");
        sb.AppendLine();
        sb.AppendLine(decision.Decision);
        sb.AppendLine();

        // Rationale
        sb.AppendLine("## Rationale");
        sb.AppendLine();
        sb.AppendLine(decision.Rationale);
        sb.AppendLine();

        // Alternatives
        if (decision.AlternativesConsidered.Count > 0)
        {
            sb.AppendLine("## Alternatives Considered");
            sb.AppendLine();
            sb.AppendLine("| Alternative | Reason Not Chosen |");
            sb.AppendLine("|---|---|");
            foreach (var (alt, reason) in decision.AlternativesConsidered)
            {
                sb.AppendLine($"| {EscapeTableCell(alt)} | {EscapeTableCell(reason)} |");
            }
            sb.AppendLine();
        }

        // Consequences
        if (decision.Consequences.Count > 0)
        {
            sb.AppendLine("## Consequences");
            sb.AppendLine();
            foreach (var consequence in decision.Consequences)
            {
                sb.AppendLine($"- {consequence}");
            }
            sb.AppendLine();
        }

        // Related
        if (decision.RelatedFiles.Count > 0 || decision.RelatedAdrs.Count > 0)
        {
            sb.AppendLine("## Related");
            sb.AppendLine();

            if (decision.RelatedFiles.Count > 0)
            {
                sb.AppendLine($"- **Files:** {string.Join(", ", decision.RelatedFiles.Select(f => $"`{f}`"))}");
            }

            if (decision.RelatedAdrs.Count > 0)
            {
                var links = decision.RelatedAdrs.Select(n => $"[ADR-{n:D4}]({n:D4}-*.md)");
                sb.AppendLine($"- **Related ADRs:** {string.Join(", ", links)}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders an ADR decision to short-form MADR markdown for minor decisions.
    /// </summary>
    public string RenderShort(AdrDecision decision)
    {
        var sb = new StringBuilder();

        // Title with inline metadata
        sb.AppendLine($"# {decision.Number}. {decision.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Date:** {decision.Date:yyyy-MM-dd} · **Status:** {decision.Status} · **Category:** {decision.Category}");
        sb.AppendLine();

        // Context as single paragraph
        sb.AppendLine(decision.Context);
        sb.AppendLine();

        // Decision and rationale inline
        sb.AppendLine($"**Decision:** {decision.Decision}");
        sb.AppendLine();
        sb.AppendLine($"**Rationale:** {decision.Rationale}");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Renders an ADR decision using the appropriate template based on category.
    /// </summary>
    public string Render(AdrDecision decision, bool useShortForm = false)
    {
        if (useShortForm || AdrCategory.IsMinor(decision.Category))
        {
            return RenderShort(decision);
        }
        return RenderFull(decision);
    }

    /// <summary>
    /// Generates the meta-ADR (0000) for recording architecture decisions.
    /// </summary>
    public string RenderMetaAdr(DateOnly date, string deciders)
    {
        return $"""
            # 0. Record architecture decisions

            **Date:** {date:yyyy-MM-dd}

            **Status:** Accepted

            **Deciders:** {deciders}

            ## Context and Problem Statement

            We need to record the architectural decisions made during project scaffolding and development.
            These decisions shape the project structure, technology choices, and development practices.
            Without documentation, the rationale behind these choices is lost over time.

            ## Decision

            We will use Architecture Decision Records (ADRs), as described by Michael Nygard in his article
            "Documenting Architecture Decisions".

            ## Rationale

            ADRs provide a lightweight, version-controlled way to document significant architectural decisions.
            They capture the context, decision, and consequences in a structured format that is easy to write,
            read, and maintain alongside the codebase.

            ## Alternatives Considered

            | Alternative | Reason Not Chosen |
            |---|---|
            | Wiki pages | Not version-controlled with code, can become stale |
            | Design documents | Too heavyweight for individual decisions |
            | No documentation | Leads to knowledge loss and repeated discussions |

            ## Consequences

            - Architecture decisions are documented close to the code
            - New team members can understand why decisions were made
            - Decisions can be revisited and superseded with clear history
            - Requires discipline to maintain ADRs as decisions evolve

            ## Related

            - **Files:** `docs/adr/`

            """;
    }

    /// <summary>
    /// Generates the blank template for future manual ADRs.
    /// </summary>
    public string RenderBlankTemplate()
    {
        return """
            # [NUMBER]. [TITLE]

            **Date:** YYYY-MM-DD

            **Status:** Proposed | Accepted | Deprecated | Superseded by [ADR-XXXX](XXXX-title.md)

            **Deciders:** [Names or roles]

            ## Context and Problem Statement

            [Describe the context and problem that led to this decision. What is the issue that we're seeing that motivates this decision?]

            ## Decision

            [State the decision that was made. Use active voice: "We will..."]

            ## Rationale

            [Explain why this option was chosen over alternatives. What makes it the best fit?]

            ## Alternatives Considered

            | Alternative | Reason Not Chosen |
            |---|---|
            | [Option A] | [Why not chosen] |
            | [Option B] | [Why not chosen] |

            ## Consequences

            - [Positive consequence 1]
            - [Positive consequence 2]
            - [Negative consequence / trade-off]

            ## Related

            - **Files:** [Affected files or directories]
            - **Related ADRs:** [Links to related decisions]

            """;
    }

    private string FormatStatus(AdrDecision decision)
    {
        if (decision.Status == AdrStatus.Superseded && decision.Supersedes.HasValue)
        {
            return $"Superseded by [ADR-{decision.Supersedes.Value:D4}]({decision.Supersedes.Value:D4}-*.md)";
        }
        return decision.Status.ToString();
    }

    private static string EscapeTableCell(string value)
    {
        return value
            .Replace("|", "\\|")
            .Replace("\n", " ")
            .Replace("\r", "");
    }
}
