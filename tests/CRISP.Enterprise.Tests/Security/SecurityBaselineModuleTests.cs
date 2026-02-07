using CRISP.Adr;
using CRISP.Enterprise.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CRISP.Enterprise.Tests.Security;

public class SecurityBaselineModuleTests : IDisposable
{
    private readonly ILogger<SecurityBaselineModule> _logger;
    private readonly DecisionCollector _decisionCollector;
    private readonly string _tempDir;

    public SecurityBaselineModuleTests()
    {
        _logger = Substitute.For<ILogger<SecurityBaselineModule>>();
        _decisionCollector = new DecisionCollector();
        _tempDir = Path.Combine(Path.GetTempPath(), $"crisp-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void ShouldRun_AlwaysReturnsTrue()
    {
        // Arrange
        var module = new SecurityBaselineModule(_logger);
        var context = CreateTestContext();

        // Act & Assert
        module.ShouldRun(context).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_CreatesSecurityMd()
    {
        // Arrange
        var module = new SecurityBaselineModule(_logger);
        var context = CreateTestContext();

        // Act
        var result = await module.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.FilesCreated.Should().Contain("SECURITY.md");

        var securityPath = Path.Combine(_tempDir, "SECURITY.md");
        File.Exists(securityPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(securityPath);
        content.Should().Contain("# Security Policy");
        content.Should().Contain("Reporting a Vulnerability");
    }

    [Fact]
    public async Task ExecuteAsync_UsesSecurityContactEmail()
    {
        // Arrange
        var module = new SecurityBaselineModule(_logger);
        var context = CreateTestContext() with
        {
            SecurityContactEmail = "security@acme.com"
        };

        // Act
        await module.ExecuteAsync(context);

        // Assert
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, "SECURITY.md"));
        content.Should().Contain("security@acme.com");
    }

    [Fact]
    public async Task ExecuteAsync_CreatesEnvExample()
    {
        // Arrange
        var module = new SecurityBaselineModule(_logger);
        var context = CreateTestContext();

        // Act
        var result = await module.ExecuteAsync(context);

        // Assert
        result.FilesCreated.Should().Contain(".env.example");

        var envPath = Path.Combine(_tempDir, ".env.example");
        File.Exists(envPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(envPath);
        content.Should().Contain("APP_NAME=test-project");
        content.Should().Contain("APP_PORT=8080");
    }

    [Fact]
    public async Task ExecuteAsync_IncludesDatabaseVarsWhenDatabaseConfigured()
    {
        // Arrange
        var module = new SecurityBaselineModule(_logger);
        var context = CreateTestContext() with
        {
            HasDatabase = true,
            DatabaseType = "postgresql"
        };

        // Act
        await module.ExecuteAsync(context);

        // Assert
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, ".env.example"));
        content.Should().Contain("DB_HOST");
        content.Should().Contain("DB_PORT");
        content.Should().Contain("5432");
    }

    [Fact]
    public async Task ExecuteAsync_AppendsToExistingGitignore()
    {
        // Arrange
        var module = new SecurityBaselineModule(_logger);
        var existingGitignore = "# Existing content\nnode_modules/\n";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, ".gitignore"), existingGitignore);

        var context = CreateTestContext();

        // Act
        await module.ExecuteAsync(context);

        // Assert
        var content = await File.ReadAllTextAsync(Path.Combine(_tempDir, ".gitignore"));
        content.Should().Contain("node_modules/"); // Existing
        content.Should().Contain("Secrets & credentials"); // New
        content.Should().Contain(".env"); // New patterns
    }

    [Fact]
    public async Task ExecuteAsync_RecordsAdr()
    {
        // Arrange
        var module = new SecurityBaselineModule(_logger);
        var context = CreateTestContext();

        // Act
        await module.ExecuteAsync(context);

        // Assert
        var decisions = _decisionCollector.GetDecisions();
        decisions.Should().ContainSingle(d => d.Category == AdrCategory.Security);
    }

    private ProjectContext CreateTestContext()
    {
        return new ProjectContext
        {
            ProjectName = "test-project",
            Language = "csharp",
            Runtime = ".NET 10",
            Framework = "ASP.NET Core Web API",
            ScmPlatform = "github",
            RepositoryUrl = "https://github.com/test/test-project",
            DefaultBranch = "main",
            WorkspacePath = _tempDir,
            DecisionCollector = _decisionCollector,
            Port = 8080
        };
    }
}
