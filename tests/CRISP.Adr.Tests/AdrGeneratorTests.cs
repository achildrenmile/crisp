using CRISP.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CRISP.Adr.Tests;

public class AdrGeneratorTests
{
    private readonly ILogger<AdrGenerator> _logger;
    private readonly IFilesystemOperations _filesystem;
    private readonly AdrTemplateEngine _templateEngine;
    private readonly AdrIndexGenerator _indexGenerator;
    private readonly Dictionary<string, string> _writtenFiles;

    public AdrGeneratorTests()
    {
        _logger = Substitute.For<ILogger<AdrGenerator>>();
        _filesystem = Substitute.For<IFilesystemOperations>();
        _templateEngine = new AdrTemplateEngine();
        _indexGenerator = new AdrIndexGenerator();
        _writtenFiles = new Dictionary<string, string>();

        // Capture written files
        _filesystem.WriteFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(x => _writtenFiles[x.Arg<string>()] = x.ArgAt<string>(1));
    }

    private AdrGenerator CreateGenerator(AdrConfiguration? config = null)
    {
        config ??= new AdrConfiguration();
        var options = Options.Create(config);
        return new AdrGenerator(_logger, options, _templateEngine, _indexGenerator, _filesystem);
    }

    [Fact]
    public async Task GenerateAsync_CreatesOutputDirectory()
    {
        // Arrange
        var generator = CreateGenerator();
        var decisions = new List<AdrDecision>();

        // Act
        await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        await _filesystem.Received().CreateDirectoryAsync(
            Arg.Is<string>(s => s.Contains("docs/adr")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_WithMetaAdrEnabled_GeneratesMetaAdr()
    {
        // Arrange
        var config = new AdrConfiguration { GenerateMetaAdr = true };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>();

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        result.Should().Contain("docs/adr/0000-record-architecture-decisions.md");
        _writtenFiles.Keys.Should().Contain(k => k.Contains("0000-record-architecture-decisions.md"));
    }

    [Fact]
    public async Task GenerateAsync_WithMetaAdrDisabled_SkipsMetaAdr()
    {
        // Arrange
        var config = new AdrConfiguration { GenerateMetaAdr = false };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>();

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        result.Should().NotContain(f => f.Contains("0000"));
        _writtenFiles.Keys.Should().NotContain(k => k.Contains("0000"));
    }

    [Fact]
    public async Task GenerateAsync_GeneratesAllDecisionFiles()
    {
        // Arrange
        var generator = CreateGenerator();
        var decisions = new List<AdrDecision>
        {
            new()
            {
                Number = 1,
                Title = "Use Python",
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "language"
            },
            new()
            {
                Number = 2,
                Title = "Use FastAPI",
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "framework"
            }
        };

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        result.Should().Contain("docs/adr/0001-use-python.md");
        result.Should().Contain("docs/adr/0002-use-fastapi.md");
    }

    [Fact]
    public async Task GenerateAsync_WithTemplateEnabled_GeneratesTemplate()
    {
        // Arrange
        var config = new AdrConfiguration { IncludeTemplate = true };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>();

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        result.Should().Contain("docs/adr/template.md");
    }

    [Fact]
    public async Task GenerateAsync_WithTemplateDisabled_SkipsTemplate()
    {
        // Arrange
        var config = new AdrConfiguration { IncludeTemplate = false };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>();

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        result.Should().NotContain(f => f.Contains("template.md"));
    }

    [Fact]
    public async Task GenerateAsync_WithIndexEnabled_GeneratesIndex()
    {
        // Arrange
        var config = new AdrConfiguration { GenerateIndex = true };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>();

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        result.Should().Contain("docs/adr/README.md");
    }

    [Fact]
    public async Task GenerateAsync_WithIndexDisabled_SkipsIndex()
    {
        // Arrange
        var config = new AdrConfiguration { GenerateIndex = false };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>();

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        result.Should().NotContain(f => f.Contains("README.md"));
    }

    [Fact]
    public async Task GenerateAsync_ReturnsCorrectFilePaths()
    {
        // Arrange
        var config = new AdrConfiguration
        {
            GenerateMetaAdr = true,
            IncludeTemplate = true,
            GenerateIndex = true
        };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>
        {
            new()
            {
                Number = 1,
                Title = "Test Decision",
                Context = "Test",
                Decision = "Test",
                Rationale = "Test",
                Category = "framework"
            }
        };

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        result.Should().HaveCount(4); // Meta + decision + template + index
        result.Should().OnlyContain(p => p.StartsWith("docs/adr/"));
    }

    [Fact]
    public async Task GenerateAsync_UsesCustomOutputDirectory()
    {
        // Arrange
        var config = new AdrConfiguration { OutputDirectory = "documentation/decisions" };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>();

        // Act
        var result = await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        await _filesystem.Received().CreateDirectoryAsync(
            Arg.Is<string>(s => s.Contains("documentation/decisions")),
            Arg.Any<CancellationToken>());
        result.Should().OnlyContain(p => p.StartsWith("documentation/decisions/"));
    }

    [Fact]
    public async Task GenerateAsync_UsesOrganizationNameInDeciders()
    {
        // Arrange
        var config = new AdrConfiguration { OrganizationName = "Acme Corp" };
        var generator = CreateGenerator(config);
        var decisions = new List<AdrDecision>();

        // Act
        await generator.GenerateAsync(decisions, "/workspace", CancellationToken.None);

        // Assert
        var metaAdrContent = _writtenFiles.First(kv => kv.Key.Contains("0000")).Value;
        metaAdrContent.Should().Contain("CRISP Agent (Acme Corp)");
    }
}
