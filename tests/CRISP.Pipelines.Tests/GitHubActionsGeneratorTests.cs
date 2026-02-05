using CRISP.Core.Enums;
using CRISP.Core.Models;
using CRISP.Pipelines.Generators;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRISP.Pipelines.Tests;

public class GitHubActionsGeneratorTests
{
    private readonly GitHubActionsGenerator _generator;

    public GitHubActionsGeneratorTests()
    {
        _generator = new GitHubActionsGenerator(NullLogger<GitHubActionsGenerator>.Instance);
    }

    [Fact]
    public void Generator_ShouldSupportGitHubPlatform()
    {
        // Assert
        _generator.Platform.Should().Be(ScmPlatform.GitHub);
        _generator.Format.Should().BeNull();
    }

    [Fact]
    public async Task GeneratePipeline_ForDotNet_ShouldGenerateValidYaml()
    {
        // Arrange
        var requirements = new ProjectRequirements
        {
            ProjectName = "test-api",
            Language = ProjectLanguage.CSharp,
            RuntimeVersion = ".NET 8",
            Framework = ProjectFramework.AspNetCoreWebApi
        };

        // Act
        var result = await _generator.GeneratePipelineAsync(requirements);

        // Assert
        result.Success.Should().BeTrue();
        result.FileName.Should().Be("ci.yml");
        result.FilePath.Should().Be(".github/workflows/ci.yml");
        result.Content.Should().Contain("dotnet");
        result.Content.Should().Contain("8.0.x");
        result.BuildSteps.Should().Contain("Build");
        result.BuildSteps.Should().Contain("Run tests");
    }

    [Fact]
    public async Task GeneratePipeline_ForPython_ShouldGenerateValidYaml()
    {
        // Arrange
        var requirements = new ProjectRequirements
        {
            ProjectName = "test-api",
            Language = ProjectLanguage.Python,
            RuntimeVersion = "3.12",
            Framework = ProjectFramework.FastApi,
            LintingTools = ["Ruff"]
        };

        // Act
        var result = await _generator.GeneratePipelineAsync(requirements);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("python");
        result.Content.Should().Contain("ruff");
        result.Content.Should().Contain("pytest");
        result.BuildSteps.Should().Contain("Lint with Ruff");
    }

    [Fact]
    public async Task GeneratePipeline_WithContainerSupport_ShouldIncludeDockerStep()
    {
        // Arrange
        var requirements = new ProjectRequirements
        {
            ProjectName = "test-api",
            Language = ProjectLanguage.CSharp,
            RuntimeVersion = ".NET 8",
            Framework = ProjectFramework.AspNetCoreWebApi,
            IncludeContainerSupport = true
        };

        // Act
        var result = await _generator.GeneratePipelineAsync(requirements);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().Contain("docker");
        result.BuildSteps.Should().Contain("Build Docker image");
    }

    [Fact]
    public async Task ValidatePipeline_WithValidYaml_ShouldPass()
    {
        // Arrange
        var validYaml = """
            name: CI
            on:
              push:
                branches: [main]
            jobs:
              build:
                runs-on: ubuntu-latest
                steps:
                  - uses: actions/checkout@v4
            """;

        // Act
        var result = await _generator.ValidatePipelineAsync(validYaml);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidatePipeline_WithInvalidYaml_ShouldFail()
    {
        // Arrange
        var invalidYaml = "name: [invalid yaml structure";

        // Act
        var result = await _generator.ValidatePipelineAsync(invalidYaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
