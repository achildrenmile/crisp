using CRISP.Core.Enums;
using CRISP.Core.Models;
using FluentAssertions;
using Xunit;

namespace CRISP.Core.Tests;

public class ProjectRequirementsTests
{
    [Fact]
    public void ProjectRequirements_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var requirements = new ProjectRequirements
        {
            ProjectName = "test-project",
            Language = ProjectLanguage.CSharp,
            RuntimeVersion = ".NET 8",
            Framework = ProjectFramework.AspNetCoreWebApi
        };

        // Assert
        requirements.ScmPlatform.Should().Be(ScmPlatform.GitHub);
        requirements.Visibility.Should().Be(RepositoryVisibility.Private);
        requirements.LintingTools.Should().BeEmpty();
        requirements.AdditionalTooling.Should().BeEmpty();
        requirements.IncludeContainerSupport.Should().BeFalse();
    }

    [Fact]
    public void ProjectRequirements_ShouldAcceptCustomValues()
    {
        // Arrange & Act
        var requirements = new ProjectRequirements
        {
            ProjectName = "custom-project",
            Description = "A custom project",
            Language = ProjectLanguage.Python,
            RuntimeVersion = "Python 3.12",
            Framework = ProjectFramework.FastApi,
            ScmPlatform = ScmPlatform.AzureDevOps,
            Visibility = RepositoryVisibility.Internal,
            LintingTools = ["Ruff"],
            TestingFramework = "pytest",
            IncludeContainerSupport = true,
            AdditionalTooling = ["pre-commit"]
        };

        // Assert
        requirements.ProjectName.Should().Be("custom-project");
        requirements.Description.Should().Be("A custom project");
        requirements.Language.Should().Be(ProjectLanguage.Python);
        requirements.Framework.Should().Be(ProjectFramework.FastApi);
        requirements.ScmPlatform.Should().Be(ScmPlatform.AzureDevOps);
        requirements.Visibility.Should().Be(RepositoryVisibility.Internal);
        requirements.LintingTools.Should().Contain("Ruff");
        requirements.TestingFramework.Should().Be("pytest");
        requirements.IncludeContainerSupport.Should().BeTrue();
        requirements.AdditionalTooling.Should().Contain("pre-commit");
    }
}
