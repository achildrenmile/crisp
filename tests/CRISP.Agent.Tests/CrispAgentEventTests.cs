using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using FluentAssertions;
using Xunit;

namespace CRISP.Agent.Tests;

public class CrispAgentEventTests
{
    [Fact]
    public void ExecutionPlanEventArgs_ShouldHoldPlan()
    {
        // Arrange
        var plan = CreateTestPlan();

        // Act
        var eventArgs = new ExecutionPlanEventArgs { Plan = plan };

        // Assert
        eventArgs.Plan.Should().BeSameAs(plan);
        eventArgs.Plan.Requirements.ProjectName.Should().Be("test-project");
    }

    [Fact]
    public void ExecutionStepEventArgs_ShouldHoldStepAndStatus()
    {
        // Arrange
        var step = new ExecutionStep
        {
            StepNumber = 1,
            Description = "Test step",
            Operation = "test.operation"
        };

        // Act
        var successEventArgs = new ExecutionStepEventArgs
        {
            Step = step,
            Success = true
        };

        var failedEventArgs = new ExecutionStepEventArgs
        {
            Step = step,
            Success = false,
            ErrorMessage = "Test error"
        };

        // Assert
        successEventArgs.Step.Should().BeSameAs(step);
        successEventArgs.Success.Should().BeTrue();
        successEventArgs.ErrorMessage.Should().BeNull();

        failedEventArgs.Success.Should().BeFalse();
        failedEventArgs.ErrorMessage.Should().Be("Test error");
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
            Summary = "Test plan",
            Steps = []
        };
    }
}
