using FluentAssertions;

namespace CRISP.Adr.Tests;

public class DecisionCollectorTests
{
    [Fact]
    public void Record_AutoIncrementsNumbersStartingFromOne()
    {
        // Arrange
        var collector = new DecisionCollector();

        // Act
        collector.Record("First", "Context", "Decision", "Rationale", "framework");
        collector.Record("Second", "Context", "Decision", "Rationale", "language");
        collector.Record("Third", "Context", "Decision", "Rationale", "testing");

        var decisions = collector.GetDecisions();

        // Assert
        decisions.Should().HaveCount(3);
        decisions[0].Number.Should().Be(1);
        decisions[1].Number.Should().Be(2);
        decisions[2].Number.Should().Be(3);
    }

    [Fact]
    public void GetDecisions_ReturnsDecisionsInOrder()
    {
        // Arrange
        var collector = new DecisionCollector();
        collector.Record("First", "Context", "Decision", "Rationale", "framework");
        collector.Record("Second", "Context", "Decision", "Rationale", "language");

        // Act
        var decisions = collector.GetDecisions();

        // Assert
        decisions[0].Title.Should().Be("First");
        decisions[1].Title.Should().Be("Second");
    }

    [Fact]
    public void Clear_ResetsStateAndCounter()
    {
        // Arrange
        var collector = new DecisionCollector();
        collector.Record("First", "Context", "Decision", "Rationale", "framework");
        collector.Record("Second", "Context", "Decision", "Rationale", "language");

        // Act
        collector.Clear();

        // Assert
        collector.Count.Should().Be(0);
        collector.GetDecisions().Should().BeEmpty();

        // New decisions start from 1 again
        collector.Record("New First", "Context", "Decision", "Rationale", "framework");
        collector.GetDecisions()[0].Number.Should().Be(1);
    }

    [Fact]
    public void Record_WithFullDecision_PreservesAllProperties()
    {
        // Arrange
        var collector = new DecisionCollector();
        var decision = new AdrDecision
        {
            Number = 0, // Will be assigned
            Title = "Test Decision",
            Context = "Test Context",
            Decision = "Test Decision",
            Rationale = "Test Rationale",
            Category = "framework",
            Status = AdrStatus.Proposed,
            AlternativesConsidered = new Dictionary<string, string> { ["Alt"] = "Reason" },
            Consequences = ["Consequence 1"],
            RelatedFiles = ["file.txt"]
        };

        // Act
        collector.Record(decision);
        var result = collector.GetDecisions()[0];

        // Assert
        result.Number.Should().Be(1);
        result.Title.Should().Be("Test Decision");
        result.Status.Should().Be(AdrStatus.Proposed);
        result.AlternativesConsidered.Should().ContainKey("Alt");
        result.Consequences.Should().Contain("Consequence 1");
        result.RelatedFiles.Should().Contain("file.txt");
    }

    [Fact]
    public void Record_WithInvalidCategory_DefaultsToOther()
    {
        // Arrange
        var collector = new DecisionCollector();

        // Act
        collector.Record("Test", "Context", "Decision", "Rationale", "invalid-category");

        // Assert
        collector.GetDecisions()[0].Category.Should().Be("other");
    }

    [Fact]
    public void Record_NormalizesCategory()
    {
        // Arrange
        var collector = new DecisionCollector();

        // Act
        collector.Record("Test", "Context", "Decision", "Rationale", "FRAMEWORK");

        // Assert
        collector.GetDecisions()[0].Category.Should().Be("framework");
    }

    [Fact]
    public void SetDeciders_AffectsNewDecisions()
    {
        // Arrange
        var collector = new DecisionCollector();
        collector.SetDeciders("Custom Team");

        // Act
        collector.Record("Test", "Context", "Decision", "Rationale", "framework");

        // Assert
        collector.GetDecisions()[0].Deciders.Should().Be("Custom Team");
    }

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        // Arrange
        var collector = new DecisionCollector();

        // Act & Assert
        collector.Count.Should().Be(0);

        collector.Record("First", "Context", "Decision", "Rationale", "framework");
        collector.Count.Should().Be(1);

        collector.Record("Second", "Context", "Decision", "Rationale", "language");
        collector.Count.Should().Be(2);
    }

    [Fact]
    public void Record_WithAlternativesAndConsequences_StoresThem()
    {
        // Arrange
        var collector = new DecisionCollector();
        var alternatives = new Dictionary<string, string>
        {
            ["Option A"] = "Too slow",
            ["Option B"] = "Too complex"
        };
        var consequences = new List<string> { "Positive 1", "Negative 1" };
        var relatedFiles = new List<string> { "main.py", "test.py" };

        // Act
        collector.Record(
            "Test",
            "Context",
            "Decision",
            "Rationale",
            "framework",
            alternatives,
            consequences,
            relatedFiles);

        var result = collector.GetDecisions()[0];

        // Assert
        result.AlternativesConsidered.Should().HaveCount(2);
        result.AlternativesConsidered["Option A"].Should().Be("Too slow");
        result.Consequences.Should().HaveCount(2);
        result.RelatedFiles.Should().HaveCount(2);
    }
}
