using FluentAssertions;

namespace CRISP.Adr.Tests;

public class AdrTemplateEngineTests
{
    private readonly AdrTemplateEngine _engine = new();

    [Fact]
    public void RenderFull_WithAllFields_GeneratesCorrectMarkdown()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 1,
            Title = "Use FastAPI as web framework",
            Status = AdrStatus.Accepted,
            Date = new DateOnly(2026, 2, 7),
            Deciders = "CRISP Agent (AI-assisted)",
            Context = "We need a web framework for the Python REST API.",
            Decision = "Use FastAPI as the web framework.",
            Rationale = "FastAPI provides automatic OpenAPI docs and async support.",
            Category = "framework",
            AlternativesConsidered = new Dictionary<string, string>
            {
                ["Flask"] = "No built-in async support",
                ["Django"] = "Too heavyweight"
            },
            Consequences = ["API follows FastAPI patterns", "Automatic documentation"],
            RelatedFiles = ["src/main.py", "requirements.txt"]
        };

        // Act
        var result = _engine.RenderFull(decision);

        // Assert
        result.Should().Contain("# 1. Use FastAPI as web framework");
        result.Should().Contain("**Date:** 2026-02-07");
        result.Should().Contain("**Status:** Accepted");
        result.Should().Contain("**Deciders:** CRISP Agent (AI-assisted)");
        result.Should().Contain("## Context and Problem Statement");
        result.Should().Contain("We need a web framework for the Python REST API.");
        result.Should().Contain("## Decision");
        result.Should().Contain("Use FastAPI as the web framework.");
        result.Should().Contain("## Rationale");
        result.Should().Contain("## Alternatives Considered");
        result.Should().Contain("| Flask | No built-in async support |");
        result.Should().Contain("| Django | Too heavyweight |");
        result.Should().Contain("## Consequences");
        result.Should().Contain("- API follows FastAPI patterns");
        result.Should().Contain("## Related");
        result.Should().Contain("`src/main.py`");
    }

    [Fact]
    public void RenderFull_WithEmptyAlternatives_OmitsAlternativesSection()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 1,
            Title = "Use Python 3.12",
            Context = "We need a runtime.",
            Decision = "Use Python 3.12.",
            Rationale = "Latest stable version.",
            Category = "language",
            AlternativesConsidered = new Dictionary<string, string>()
        };

        // Act
        var result = _engine.RenderFull(decision);

        // Assert
        result.Should().NotContain("## Alternatives Considered");
    }

    [Fact]
    public void RenderShort_GeneratesCompactFormat()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 5,
            Title = "Use Ruff for linting",
            Status = AdrStatus.Accepted,
            Date = new DateOnly(2026, 2, 7),
            Context = "We need a linter for Python code.",
            Decision = "Use Ruff.",
            Rationale = "Ruff is fast and comprehensive.",
            Category = "code-style"
        };

        // Act
        var result = _engine.RenderShort(decision);

        // Assert
        result.Should().Contain("# 5. Use Ruff for linting");
        result.Should().Contain("**Date:** 2026-02-07 · **Status:** Accepted · **Category:** code-style");
        result.Should().Contain("**Decision:** Use Ruff.");
        result.Should().Contain("**Rationale:** Ruff is fast and comprehensive.");
        result.Should().NotContain("## Context and Problem Statement");
    }

    [Fact]
    public void Render_WithMinorCategory_UsesShortForm()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 1,
            Title = "Use EditorConfig",
            Context = "Need consistent formatting.",
            Decision = "Use EditorConfig.",
            Rationale = "Cross-editor support.",
            Category = "code-style" // Minor category
        };

        // Act
        var result = _engine.Render(decision);

        // Assert
        // Short form uses inline format
        result.Should().Contain("**Date:**");
        result.Should().Contain("· **Status:**");
        result.Should().NotContain("## Context and Problem Statement");
    }

    [Fact]
    public void Render_WithMajorCategory_UsesFullForm()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 1,
            Title = "Use FastAPI",
            Context = "Need a framework.",
            Decision = "Use FastAPI.",
            Rationale = "Best for async.",
            Category = "framework" // Major category
        };

        // Act
        var result = _engine.Render(decision);

        // Assert
        result.Should().Contain("## Context and Problem Statement");
    }

    [Fact]
    public void RenderFull_WithSupersededStatus_FormatsLinkCorrectly()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 1,
            Title = "Use Flask",
            Status = AdrStatus.Superseded,
            Supersedes = 2,
            Context = "Old decision.",
            Decision = "Use Flask.",
            Rationale = "Was good.",
            Category = "framework"
        };

        // Act
        var result = _engine.RenderFull(decision);

        // Assert
        result.Should().Contain("Superseded by [ADR-0002]");
    }

    [Fact]
    public void RenderMetaAdr_GeneratesStandardContent()
    {
        // Arrange
        var date = new DateOnly(2026, 2, 7);
        var deciders = "Test Team";

        // Act
        var result = _engine.RenderMetaAdr(date, deciders);

        // Assert
        result.Should().Contain("# 0. Record architecture decisions");
        result.Should().Contain("**Date:** 2026-02-07");
        result.Should().Contain("**Deciders:** Test Team");
        result.Should().Contain("Architecture Decision Records (ADRs)");
        result.Should().Contain("Michael Nygard");
    }

    [Fact]
    public void RenderBlankTemplate_ContainsPlaceholders()
    {
        // Act
        var result = _engine.RenderBlankTemplate();

        // Assert
        result.Should().Contain("[NUMBER]");
        result.Should().Contain("[TITLE]");
        result.Should().Contain("YYYY-MM-DD");
        result.Should().Contain("Proposed | Accepted | Deprecated | Superseded");
        result.Should().Contain("[Describe the context");
    }

    [Fact]
    public void RenderFull_EscapesPipeInTableCells()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 1,
            Title = "Test",
            Context = "Test",
            Decision = "Test",
            Rationale = "Test",
            Category = "other",
            AlternativesConsidered = new Dictionary<string, string>
            {
                ["Option A | B"] = "Contains | pipe"
            }
        };

        // Act
        var result = _engine.RenderFull(decision);

        // Assert
        result.Should().Contain(@"Option A \| B");
        result.Should().Contain(@"Contains \| pipe");
    }
}
