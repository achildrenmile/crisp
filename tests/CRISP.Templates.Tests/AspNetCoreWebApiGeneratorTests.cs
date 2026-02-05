using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using CRISP.Templates.Generators;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRISP.Templates.Tests;

public class AspNetCoreWebApiGeneratorTests
{
    private readonly AspNetCoreWebApiGenerator _generator;
    private readonly IFilesystemOperations _filesystem;

    public AspNetCoreWebApiGeneratorTests()
    {
        _filesystem = Substitute.For<IFilesystemOperations>();
        _filesystem.CreateDirectoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _filesystem.WriteFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _generator = new AspNetCoreWebApiGenerator(
            NullLogger<AspNetCoreWebApiGenerator>.Instance,
            _filesystem);
    }

    [Fact]
    public void Generator_ShouldHaveCorrectTemplateId()
    {
        // Assert
        _generator.TemplateId.Should().Be("aspnetcore-webapi");
        _generator.TemplateName.Should().Be("ASP.NET Core Web API");
        _generator.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void SupportsRequirements_ShouldReturnTrue_ForCSharpWebApi()
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
        var result = _generator.SupportsRequirements(requirements);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsRequirements_ShouldReturnFalse_ForPythonFastApi()
    {
        // Arrange
        var requirements = new ProjectRequirements
        {
            ProjectName = "test-api",
            Language = ProjectLanguage.Python,
            RuntimeVersion = "3.12",
            Framework = ProjectFramework.FastApi
        };

        // Act
        var result = _generator.SupportsRequirements(requirements);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlannedFilesAsync_ShouldReturnExpectedFiles()
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
        var files = await _generator.GetPlannedFilesAsync(requirements);

        // Assert
        files.Should().NotBeEmpty();
        files.Should().Contain(f => f.RelativePath == "test-api.sln");
        files.Should().Contain(f => f.RelativePath.Contains("Program.cs"));
        files.Should().Contain(f => f.RelativePath == ".gitignore");
        files.Should().Contain(f => f.RelativePath == "README.md");
    }

    [Fact]
    public async Task GetPlannedFilesAsync_WithContainerSupport_ShouldIncludeDockerfile()
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
        var files = await _generator.GetPlannedFilesAsync(requirements);

        // Assert
        files.Should().Contain(f => f.RelativePath == "Dockerfile");
        files.Should().Contain(f => f.RelativePath == ".dockerignore");
    }
}
