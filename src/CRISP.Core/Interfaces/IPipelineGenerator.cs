using CRISP.Core.Enums;
using CRISP.Core.Models;

namespace CRISP.Core.Interfaces;

/// <summary>
/// Pipeline generator for CI/CD configuration.
/// </summary>
public interface IPipelineGenerator
{
    /// <summary>
    /// Gets the platform this generator supports.
    /// </summary>
    ScmPlatform Platform { get; }

    /// <summary>
    /// Gets the pipeline format (for Azure DevOps).
    /// </summary>
    PipelineFormat? Format { get; }

    /// <summary>
    /// Generates a CI/CD pipeline configuration.
    /// </summary>
    /// <param name="requirements">Project requirements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pipeline content and metadata.</returns>
    Task<PipelineGenerationResult> GeneratePipelineAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a pipeline configuration.
    /// </summary>
    /// <param name="pipelineContent">Pipeline content to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<PipelineValidationResult> ValidatePipelineAsync(
        string pipelineContent,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of pipeline generation.
/// </summary>
public sealed class PipelineGenerationResult
{
    /// <summary>
    /// Whether generation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Generated pipeline content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Pipeline file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Pipeline file path relative to repository root.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Human-readable description of the pipeline.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Build steps in the pipeline.
    /// </summary>
    public required IReadOnlyList<string> BuildSteps { get; init; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of pipeline validation.
/// </summary>
public sealed class PipelineValidationResult
{
    /// <summary>
    /// Whether the pipeline is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
