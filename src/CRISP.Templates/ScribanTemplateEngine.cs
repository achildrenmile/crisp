using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using CRISP.Templates.Generators;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;

namespace CRISP.Templates;

/// <summary>
/// Template engine implementation using Scriban.
/// </summary>
public sealed class ScribanTemplateEngine : ITemplateEngine
{
    private readonly ILogger<ScribanTemplateEngine> _logger;
    private readonly IFilesystemOperations _filesystem;
    private readonly Dictionary<string, IProjectGenerator> _generators;

    public ScribanTemplateEngine(
        ILogger<ScribanTemplateEngine> logger,
        IFilesystemOperations filesystem,
        IEnumerable<IProjectGenerator> generators)
    {
        _logger = logger;
        _filesystem = filesystem;
        _generators = generators.ToDictionary(g => g.TemplateId);
    }

    public Task<IReadOnlyList<TemplateSelection>> GetAvailableTemplatesAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        var templates = new List<TemplateSelection>();

        foreach (var generator in _generators.Values)
        {
            if (generator.SupportsRequirements(requirements))
            {
                templates.Add(new TemplateSelection
                {
                    TemplateId = generator.TemplateId,
                    Name = generator.TemplateName,
                    Version = generator.Version,
                    Source = "built-in"
                });
            }
        }

        _logger.LogInformation("Found {Count} matching templates for requirements", templates.Count);
        return Task.FromResult<IReadOnlyList<TemplateSelection>>(templates);
    }

    public async Task<ScaffoldingResult> ScaffoldProjectAsync(
        TemplateSelection template,
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scaffolding project {ProjectName} using template {TemplateId}",
            requirements.ProjectName, template.TemplateId);

        if (!_generators.TryGetValue(template.TemplateId, out var generator))
        {
            return new ScaffoldingResult
            {
                Success = false,
                WorkspacePath = outputPath,
                CreatedFiles = [],
                ErrorMessage = $"Template '{template.TemplateId}' not found"
            };
        }

        try
        {
            // Ensure output directory exists
            await _filesystem.CreateDirectoryAsync(outputPath, cancellationToken);

            // Generate the project
            var createdFiles = await generator.GenerateAsync(requirements, outputPath, cancellationToken);

            _logger.LogInformation("Scaffolding completed. Created {Count} files", createdFiles.Count);

            return new ScaffoldingResult
            {
                Success = true,
                WorkspacePath = outputPath,
                CreatedFiles = createdFiles
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scaffold project");
            return new ScaffoldingResult
            {
                Success = false,
                WorkspacePath = outputPath,
                CreatedFiles = [],
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<string> RenderTemplateAsync(
        string templateContent,
        IDictionary<string, object?> context,
        CancellationToken cancellationToken = default)
    {
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"Template parsing failed: {string.Join(", ", template.Messages)}");
        }

        var scriptObject = new ScriptObject();
        foreach (var (key, value) in context)
        {
            scriptObject[key] = value;
        }

        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        var result = template.Render(templateContext);
        return Task.FromResult(result);
    }

    public async Task<IReadOnlyList<PlannedFile>> GetPlannedFilesAsync(
        TemplateSelection template,
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        if (!_generators.TryGetValue(template.TemplateId, out var generator))
        {
            return [];
        }

        return await generator.GetPlannedFilesAsync(requirements, cancellationToken);
    }
}

/// <summary>
/// Interface for project generators.
/// </summary>
public interface IProjectGenerator
{
    /// <summary>
    /// Unique template identifier.
    /// </summary>
    string TemplateId { get; }

    /// <summary>
    /// Human-readable template name.
    /// </summary>
    string TemplateName { get; }

    /// <summary>
    /// Template version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Checks if this generator supports the given requirements.
    /// </summary>
    bool SupportsRequirements(ProjectRequirements requirements);

    /// <summary>
    /// Generates the project files.
    /// </summary>
    Task<IReadOnlyList<string>> GenerateAsync(
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of files that would be created.
    /// </summary>
    Task<IReadOnlyList<PlannedFile>> GetPlannedFilesAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default);
}
