using CRISP.Core.Enums;
using CRISP.Core.Models;
using FluentAssertions;

namespace CRISP.Core.Tests;

public class ExecutionPlanTests
{
    [Fact]
    public void ExecutionPlan_ShouldGenerateUniqueId()
    {
        // Arrange & Act
        var plan1 = CreateTestPlan();
        var plan2 = CreateTestPlan();

        // Assert
        plan1.Id.Should().NotBe(Guid.Empty);
        plan2.Id.Should().NotBe(Guid.Empty);
        plan1.Id.Should().NotBe(plan2.Id);
    }

    [Fact]
    public void ExecutionPlan_ShouldSetCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var plan = CreateTestPlan();

        // Assert
        var after = DateTimeOffset.UtcNow;
        plan.CreatedAt.Should().BeOnOrAfter(before);
        plan.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void ExecutionPlan_IsApproved_ShouldDefaultToFalse()
    {
        // Act
        var plan = CreateTestPlan();

        // Assert
        plan.IsApproved.Should().BeFalse();
    }

    [Fact]
    public void ExecutionStep_IsCompleted_ShouldDefaultToFalse()
    {
        // Act
        var step = new ExecutionStep
        {
            StepNumber = 1,
            Description = "Test step",
            Operation = "test.operation"
        };

        // Assert
        step.IsCompleted.Should().BeFalse();
        step.Result.Should().BeNull();
    }

    [Fact]
    public void PolicyValidationResult_ShouldHoldValidationState()
    {
        // Act
        var passedResult = new PolicyValidationResult
        {
            PolicyId = "test-policy",
            PolicyName = "Test Policy",
            Passed = true,
            Message = "Policy passed"
        };

        var failedResult = new PolicyValidationResult
        {
            PolicyId = "test-policy-2",
            PolicyName = "Test Policy 2",
            Passed = false,
            Message = "Policy failed",
            Severity = "error"
        };

        // Assert
        passedResult.Passed.Should().BeTrue();
        failedResult.Passed.Should().BeFalse();
        failedResult.Severity.Should().Be("error");
    }

    private static ExecutionPlan CreateTestPlan()
    {
        return new ExecutionPlan
        {
            Requirements = new ProjectRequirements
            {
                ProjectName = "test-project",
                Language = ProjectLanguage.CSharp,
                RuntimeVersion = ".NET 8",
                Framework = ProjectFramework.AspNetCoreWebApi
            },
            Template = new TemplateSelection
            {
                TemplateId = "aspnetcore-webapi",
                Name = "ASP.NET Core Web API",
                Version = "1.0.0",
                Source = "built-in"
            },
            PlannedFiles = [],
            Repository = new RepositoryDetails
            {
                Name = "test-project",
                Owner = "test-owner",
                Visibility = "private",
                DefaultBranch = "main"
            },
            PolicyResults = [],
            Summary = "Test plan summary",
            Steps = []
        };
    }
}
