using CRISP.Core.Models;

namespace CRISP.Core.Interfaces;

/// <summary>
/// Template engine for project scaffolding.
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Gets available templates matching the requirements.
    /// </summary>
    /// <param name="requirements">Project requirements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching templates.</returns>
    Task<IReadOnlyList<TemplateSelection>> GetAvailableTemplatesAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scaffolds a project using the specified template.
    /// </summary>
    /// <param name="template">Template to use.</param>
    /// <param name="requirements">Project requirements.</param>
    /// <param name="outputPath">Output directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scaffolding result.</returns>
    Task<ScaffoldingResult> ScaffoldProjectAsync(
        TemplateSelection template,
        ProjectRequirements requirements,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a single template file with the given context.
    /// </summary>
    /// <param name="templateContent">Template content.</param>
    /// <param name="context">Template context variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rendered content.</returns>
    Task<string> RenderTemplateAsync(
        string templateContent,
        IDictionary<string, object?> context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of files that would be created for the given template and requirements.
    /// </summary>
    /// <param name="template">Template to use.</param>
    /// <param name="requirements">Project requirements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of planned files.</returns>
    Task<IReadOnlyList<PlannedFile>> GetPlannedFilesAsync(
        TemplateSelection template,
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default);
}
