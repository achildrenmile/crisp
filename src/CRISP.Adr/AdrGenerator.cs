using CRISP.Adr.Interfaces;
using CRISP.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRISP.Adr;

/// <summary>
/// Orchestrates the generation of all ADR files for a scaffolded project.
/// </summary>
public sealed class AdrGenerator : IAdrGenerator
{
    private readonly ILogger<AdrGenerator> _logger;
    private readonly AdrConfiguration _config;
    private readonly AdrTemplateEngine _templateEngine;
    private readonly AdrIndexGenerator _indexGenerator;
    private readonly IFilesystemOperations _filesystem;

    public AdrGenerator(
        ILogger<AdrGenerator> logger,
        IOptions<AdrConfiguration> config,
        AdrTemplateEngine templateEngine,
        AdrIndexGenerator indexGenerator,
        IFilesystemOperations filesystem)
    {
        _logger = logger;
        _config = config.Value;
        _templateEngine = templateEngine;
        _indexGenerator = indexGenerator;
        _filesystem = filesystem;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GenerateAsync(
        IReadOnlyList<AdrDecision> decisions,
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        var createdFiles = new List<string>();
        var outputDir = Path.Combine(workspacePath, _config.OutputDirectory);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _logger.LogInformation(
            "Generating {Count} ADRs to {OutputDir}",
            decisions.Count + (_config.GenerateMetaAdr ? 1 : 0),
            _config.OutputDirectory);

        // Create output directory
        await _filesystem.CreateDirectoryAsync(outputDir, cancellationToken);

        // Generate meta-ADR (0000)
        if (_config.GenerateMetaAdr)
        {
            var metaAdrPath = Path.Combine(outputDir, "0000-record-architecture-decisions.md");
            var metaAdrContent = _templateEngine.RenderMetaAdr(today, _config.GetDecidersString());
            await _filesystem.WriteFileAsync(metaAdrPath, metaAdrContent, cancellationToken);
            createdFiles.Add(Path.Combine(_config.OutputDirectory, "0000-record-architecture-decisions.md"));
            _logger.LogDebug("Generated meta-ADR: 0000-record-architecture-decisions.md");
        }

        // Generate each decision ADR
        foreach (var decision in decisions.OrderBy(d => d.Number))
        {
            var fileName = decision.GetFileName();
            var filePath = Path.Combine(outputDir, fileName);
            var content = _templateEngine.Render(decision);
            await _filesystem.WriteFileAsync(filePath, content, cancellationToken);
            createdFiles.Add(Path.Combine(_config.OutputDirectory, fileName));
            _logger.LogDebug("Generated ADR: {FileName}", fileName);
        }

        // Generate blank template
        if (_config.IncludeTemplate)
        {
            var templatePath = Path.Combine(outputDir, "template.md");
            var templateContent = _templateEngine.RenderBlankTemplate();
            await _filesystem.WriteFileAsync(templatePath, templateContent, cancellationToken);
            createdFiles.Add(Path.Combine(_config.OutputDirectory, "template.md"));
            _logger.LogDebug("Generated ADR template");
        }

        // Generate index
        if (_config.GenerateIndex)
        {
            var indexPath = Path.Combine(outputDir, "README.md");
            var indexContent = _indexGenerator.Generate(decisions, _config.GenerateMetaAdr, today);
            await _filesystem.WriteFileAsync(indexPath, indexContent, cancellationToken);
            createdFiles.Add(Path.Combine(_config.OutputDirectory, "README.md"));
            _logger.LogDebug("Generated ADR index");
        }

        _logger.LogInformation(
            "Successfully generated {Count} ADR files",
            createdFiles.Count);

        return createdFiles;
    }
}
