using FluentAssertions;

namespace CRISP.Adr.Tests;

public class AdrDecisionTests
{
    [Theory]
    [InlineData("Use FastAPI as web framework", "use-fastapi-as-web-framework")]
    [InlineData("Use Python 3.12", "use-python-3-12")]
    [InlineData("Don't use Flask", "dont-use-flask")]
    [InlineData("Use C# / .NET", "use-c-net")]
    [InlineData("Use  multiple  spaces", "use-multiple-spaces")]
    [InlineData("Use_underscores_here", "use-underscores-here")]
    [InlineData("Use (parentheses)", "use-parentheses")]
    [InlineData("Path/With/Slashes", "path-with-slashes")]
    public void GetFileNameSlug_GeneratesCorrectSlug(string title, string expectedSlug)
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 1,
            Title = title,
            Context = "Test",
            Decision = "Test",
            Rationale = "Test",
            Category = "other"
        };

        // Act
        var slug = decision.GetFileNameSlug();

        // Assert
        slug.Should().Be(expectedSlug);
    }

    [Fact]
    public void GetFileName_FormatsWithPaddedNumber()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 5,
            Title = "Use FastAPI",
            Context = "Test",
            Decision = "Test",
            Rationale = "Test",
            Category = "framework"
        };

        // Act
        var fileName = decision.GetFileName();

        // Assert
        fileName.Should().Be("0005-use-fastapi.md");
    }

    [Fact]
    public void GetFileName_HandlesLargeNumbers()
    {
        // Arrange
        var decision = new AdrDecision
        {
            Number = 123,
            Title = "Some decision",
            Context = "Test",
            Decision = "Test",
            Rationale = "Test",
            Category = "other"
        };

        // Act
        var fileName = decision.GetFileName();

        // Assert
        fileName.Should().Be("0123-some-decision.md");
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var decision = new AdrDecision
        {
            Title = "Test",
            Context = "Test",
            Decision = "Test",
            Rationale = "Test",
            Category = "other"
        };

        // Assert
        decision.Status.Should().Be(AdrStatus.Accepted);
        decision.Date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        decision.Deciders.Should().Be("CRISP Agent (AI-assisted)");
        decision.AlternativesConsidered.Should().BeEmpty();
        decision.Consequences.Should().BeEmpty();
        decision.RelatedFiles.Should().BeEmpty();
        decision.RelatedAdrs.Should().BeEmpty();
        decision.Supersedes.Should().BeNull();
    }
}
