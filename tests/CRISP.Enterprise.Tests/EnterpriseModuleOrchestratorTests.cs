using CRISP.Adr;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CRISP.Enterprise.Tests;

public class EnterpriseModuleOrchestratorTests
{
    private readonly ILogger<EnterpriseModuleOrchestrator> _logger;
    private readonly DecisionCollector _decisionCollector;

    public EnterpriseModuleOrchestratorTests()
    {
        _logger = Substitute.For<ILogger<EnterpriseModuleOrchestrator>>();
        _decisionCollector = new DecisionCollector();
    }

    [Fact]
    public async Task ExecuteAllAsync_RunsModulesInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();

        var module1 = CreateMockModule("module-1", "Module 1", 200, () => executionOrder.Add("module-1"));
        var module2 = CreateMockModule("module-2", "Module 2", 100, () => executionOrder.Add("module-2"));
        var module3 = CreateMockModule("module-3", "Module 3", 300, () => executionOrder.Add("module-3"));

        var orchestrator = CreateOrchestrator([module1, module2, module3]);
        var context = CreateTestContext();

        // Act
        await orchestrator.ExecuteAllAsync(context);

        // Assert
        executionOrder.Should().Equal("module-2", "module-1", "module-3");
    }

    [Fact]
    public async Task ExecuteAllAsync_SkipsDisabledModules()
    {
        // Arrange
        var executedModules = new List<string>();

        var module1 = CreateMockModule("enabled-module", "Enabled", 100, () => executedModules.Add("enabled-module"));
        var module2 = CreateMockModule("disabled-module", "Disabled", 200, () => executedModules.Add("disabled-module"));

        var config = new EnterpriseConfiguration
        {
            DisabledModules = ["disabled-module"]
        };

        var orchestrator = CreateOrchestrator([module1, module2], config);
        var context = CreateTestContext();

        // Act
        await orchestrator.ExecuteAllAsync(context);

        // Assert
        executedModules.Should().Equal("enabled-module");
    }

    [Fact]
    public async Task ExecuteAllAsync_SkipsModulesWhenShouldRunReturnsFalse()
    {
        // Arrange
        var executedModules = new List<string>();

        var module1 = CreateMockModule("runs", "Runs", 100, () => executedModules.Add("runs"), shouldRun: true);
        var module2 = CreateMockModule("skips", "Skips", 200, () => executedModules.Add("skips"), shouldRun: false);

        var orchestrator = CreateOrchestrator([module1, module2]);
        var context = CreateTestContext();

        // Act
        await orchestrator.ExecuteAllAsync(context);

        // Assert
        executedModules.Should().Equal("runs");
    }

    [Fact]
    public async Task ExecuteAllAsync_ContinuesAfterModuleFailure()
    {
        // Arrange
        var executedModules = new List<string>();

        var failingModule = Substitute.For<IEnterpriseModule>();
        failingModule.Id.Returns("failing");
        failingModule.DisplayName.Returns("Failing Module");
        failingModule.Order.Returns(100);
        failingModule.ShouldRun(Arg.Any<ProjectContext>()).Returns(true);
        failingModule.ExecuteAsync(Arg.Any<ProjectContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ModuleResult>(new Exception("Test failure")));

        var successModule = CreateMockModule("success", "Success", 200, () => executedModules.Add("success"));

        var orchestrator = CreateOrchestrator([failingModule, successModule]);
        var context = CreateTestContext();

        // Act
        var results = await orchestrator.ExecuteAllAsync(context);

        // Assert
        results.Should().HaveCount(2);
        results[0].Success.Should().BeFalse();
        results[0].ErrorMessage.Should().Be("Test failure");
        results[1].Success.Should().BeTrue();
        executedModules.Should().Contain("success");
    }

    [Fact]
    public async Task ExecuteAllAsync_AccumulatesGeneratedFiles()
    {
        // Arrange
        var module1 = Substitute.For<IEnterpriseModule>();
        module1.Id.Returns("module-1");
        module1.DisplayName.Returns("Module 1");
        module1.Order.Returns(100);
        module1.ShouldRun(Arg.Any<ProjectContext>()).Returns(true);
        module1.ExecuteAsync(Arg.Any<ProjectContext>(), Arg.Any<CancellationToken>())
            .Returns(new ModuleResult
            {
                ModuleId = "module-1",
                Success = true,
                FilesCreated = ["file1.md", "file2.md"]
            });

        var module2 = Substitute.For<IEnterpriseModule>();
        module2.Id.Returns("module-2");
        module2.DisplayName.Returns("Module 2");
        module2.Order.Returns(200);
        module2.ShouldRun(Arg.Any<ProjectContext>()).Returns(true);
        module2.ExecuteAsync(Arg.Any<ProjectContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var ctx = callInfo.Arg<ProjectContext>();
                // Module 2 should see files from Module 1
                ctx.GeneratedFiles.Should().Contain("file1.md");
                ctx.GeneratedFiles.Should().Contain("file2.md");

                return new ModuleResult
                {
                    ModuleId = "module-2",
                    Success = true,
                    FilesCreated = ["file3.md"]
                };
            });

        var orchestrator = CreateOrchestrator([module1, module2]);
        var context = CreateTestContext();

        // Act
        await orchestrator.ExecuteAllAsync(context);

        // Assert
        context.GeneratedFiles.Should().Contain("file1.md", "file2.md", "file3.md");
    }

    [Fact]
    public async Task ExecuteAllAsync_InvokesProgressCallback()
    {
        // Arrange
        var progressUpdates = new List<(string ModuleId, string Status)>();

        var module = CreateMockModule("test-module", "Test", 100, () => { });
        var orchestrator = CreateOrchestrator([module]);
        var context = CreateTestContext();

        // Act
        await orchestrator.ExecuteAllAsync(context, (id, status) => progressUpdates.Add((id, status)));

        // Assert
        progressUpdates.Should().Contain(("test-module", "running"));
        progressUpdates.Should().Contain(("test-module", "completed"));
    }

    [Fact]
    public void GetApplicableModules_ReturnsOrderedFilteredModules()
    {
        // Arrange
        var module1 = CreateMockModule("module-1", "Module 1", 300, () => { }, shouldRun: true);
        var module2 = CreateMockModule("module-2", "Module 2", 100, () => { }, shouldRun: true);
        var module3 = CreateMockModule("module-3", "Module 3", 200, () => { }, shouldRun: false);

        var orchestrator = CreateOrchestrator([module1, module2, module3]);
        var context = CreateTestContext();

        // Act
        var applicable = orchestrator.GetApplicableModules(context);

        // Assert
        applicable.Should().HaveCount(2);
        applicable[0].Id.Should().Be("module-2"); // Order 100
        applicable[1].Id.Should().Be("module-1"); // Order 300
    }

    private EnterpriseModuleOrchestrator CreateOrchestrator(
        IEnumerable<IEnterpriseModule> modules,
        EnterpriseConfiguration? config = null)
    {
        var options = Options.Create(config ?? new EnterpriseConfiguration());
        return new EnterpriseModuleOrchestrator(modules, options, _logger);
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
            WorkspacePath = Path.GetTempPath(),
            DecisionCollector = _decisionCollector
        };
    }

    private static IEnterpriseModule CreateMockModule(
        string id,
        string displayName,
        int order,
        Action onExecute,
        bool shouldRun = true)
    {
        var module = Substitute.For<IEnterpriseModule>();
        module.Id.Returns(id);
        module.DisplayName.Returns(displayName);
        module.Order.Returns(order);
        module.ShouldRun(Arg.Any<ProjectContext>()).Returns(shouldRun);
        module.ExecuteAsync(Arg.Any<ProjectContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                onExecute();
                return new ModuleResult { ModuleId = id, Success = true };
            });

        return module;
    }
}
