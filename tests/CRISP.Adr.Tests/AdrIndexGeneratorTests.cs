using FluentAssertions;

namespace CRISP.Adr.Tests;

public class AdrIndexGeneratorTests
{
    private readonly AdrIndexGenerator _generator = new();

    [Fact]
    public void Generate_WithDecisions_CreatesCorrectTable()
    {
        // Arrange
        var decisions = new List<AdrDecision>
        {
            new()
            {
                Number = 1,
                Title = "Use Python",
                Status = AdrStatus.Accepted,
                Date = new DateOnly(2026, 2, 7),
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "language"
            },
            new()
            {
                Number = 2,
                Title = "Use FastAPI",
                Status = AdrStatus.Accepted,
                Date = new DateOnly(2026, 2, 7),
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "framework"
            }
        };

        // Act
        var result = _generator.Generate(decisions, includeMetaAdr: true, metaAdrDate: new DateOnly(2026, 2, 7));

        // Assert
        result.Should().Contain("# Architecture Decision Records");
        result.Should().Contain("| # | Title | Status | Date | Category |");
        result.Should().Contain("| [0000](0000-record-architecture-decisions.md) | Record architecture decisions | Accepted | 2026-02-07 | process |");
        result.Should().Contain("| [0001](0001-use-python.md) | Use Python | Accepted | 2026-02-07 | language |");
        result.Should().Contain("| [0002](0002-use-fastapi.md) | Use FastAPI | Accepted | 2026-02-07 | framework |");
    }

    [Fact]
    public void Generate_WithoutMetaAdr_OmitsMetaRow()
    {
        // Arrange
        var decisions = new List<AdrDecision>
        {
            new()
            {
                Number = 1,
                Title = "Use Python",
                Status = AdrStatus.Accepted,
                Date = new DateOnly(2026, 2, 7),
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "language"
            }
        };

        // Act
        var result = _generator.Generate(decisions, includeMetaAdr: false);

        // Assert
        result.Should().NotContain("0000-record-architecture-decisions.md");
        result.Should().Contain("0001-use-python.md");
    }

    [Fact]
    public void Generate_SortsDecisionsByNumber()
    {
        // Arrange
        var decisions = new List<AdrDecision>
        {
            new()
            {
                Number = 3,
                Title = "Third",
                Status = AdrStatus.Accepted,
                Date = new DateOnly(2026, 2, 7),
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "other"
            },
            new()
            {
                Number = 1,
                Title = "First",
                Status = AdrStatus.Accepted,
                Date = new DateOnly(2026, 2, 7),
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "other"
            },
            new()
            {
                Number = 2,
                Title = "Second",
                Status = AdrStatus.Accepted,
                Date = new DateOnly(2026, 2, 7),
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "other"
            }
        };

        // Act
        var result = _generator.Generate(decisions, includeMetaAdr: false);

        // Assert
        var firstIndex = result.IndexOf("0001-first.md");
        var secondIndex = result.IndexOf("0002-second.md");
        var thirdIndex = result.IndexOf("0003-third.md");

        firstIndex.Should().BeLessThan(secondIndex);
        secondIndex.Should().BeLessThan(thirdIndex);
    }

    [Fact]
    public void Generate_IncludesCrispAttribution()
    {
        // Arrange
        var decisions = new List<AdrDecision>();

        // Act
        var result = _generator.Generate(decisions, includeMetaAdr: false);

        // Assert
        result.Should().Contain("CRISP");
        result.Should().Contain("template.md");
    }

    [Fact]
    public void Generate_WithEmptyDecisions_CreatesValidIndex()
    {
        // Arrange
        var decisions = new List<AdrDecision>();

        // Act
        var result = _generator.Generate(decisions, includeMetaAdr: true);

        // Assert
        result.Should().Contain("# Architecture Decision Records");
        result.Should().Contain("| # | Title | Status | Date | Category |");
        result.Should().Contain("0000-record-architecture-decisions.md");
    }
}
