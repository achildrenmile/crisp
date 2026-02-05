namespace CRISP.Core.Models;

/// <summary>
/// Represents the execution plan generated during Phase 2 (Planning).
/// </summary>
public sealed class ExecutionPlan
{
    /// <summary>
    /// Unique identifier for this execution plan.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the plan was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The project requirements this plan is based on.
    /// </summary>
    public required ProjectRequirements Requirements { get; init; }

    /// <summary>
    /// Template to use for scaffolding.
    /// </summary>
    public required TemplateSelection Template { get; init; }

    /// <summary>
    /// Files and directories to be created.
    /// </summary>
    public required IReadOnlyList<PlannedFile> PlannedFiles { get; init; }

    /// <summary>
    /// Repository creation details.
    /// </summary>
    public required RepositoryDetails Repository { get; init; }

    /// <summary>
    /// CI/CD pipeline definition.
    /// </summary>
    public PipelineDefinition? Pipeline { get; init; }

    /// <summary>
    /// Policy validation results.
    /// </summary>
    public required IReadOnlyList<PolicyValidationResult> PolicyResults { get; init; }

    /// <summary>
    /// Whether the plan has been approved by the developer.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Human-readable summary of the plan.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Ordered list of execution steps.
    /// </summary>
    public required IReadOnlyList<ExecutionStep> Steps { get; init; }
}

/// <summary>
/// Represents a template selection.
/// </summary>
public sealed class TemplateSelection
{
    /// <summary>
    /// Template identifier.
    /// </summary>
    public required string TemplateId { get; init; }

    /// <summary>
    /// Template display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Template version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Source of the template (built-in, repository URL, etc.).
    /// </summary>
    public required string Source { get; init; }
}

/// <summary>
/// Represents a file to be created during scaffolding.
/// </summary>
public sealed class PlannedFile
{
    /// <summary>
    /// Relative path within the project.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Whether this is a directory.
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// Description of the file's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Template file path (if generated from template).
    /// </summary>
    public string? TemplateFile { get; init; }
}

/// <summary>
/// Repository creation details.
/// </summary>
public sealed class RepositoryDetails
{
    /// <summary>
    /// Repository name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full repository URL (after creation).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Clone URL.
    /// </summary>
    public string? CloneUrl { get; set; }

    /// <summary>
    /// Organization or owner.
    /// </summary>
    public required string Owner { get; init; }

    /// <summary>
    /// Repository visibility.
    /// </summary>
    public required string Visibility { get; init; }

    /// <summary>
    /// Default branch name.
    /// </summary>
    public required string DefaultBranch { get; init; }
}

/// <summary>
/// CI/CD pipeline definition.
/// </summary>
public sealed class PipelineDefinition
{
    /// <summary>
    /// Pipeline file name (e.g., "ci.yml", "azure-pipelines.yml").
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Pipeline file path relative to repository root.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Trigger configuration description.
    /// </summary>
    public required string TriggerDescription { get; init; }

    /// <summary>
    /// Build steps description.
    /// </summary>
    public required IReadOnlyList<string> BuildSteps { get; init; }
}

/// <summary>
/// Policy validation result.
/// </summary>
public sealed class PolicyValidationResult
{
    /// <summary>
    /// Policy rule identifier.
    /// </summary>
    public required string PolicyId { get; init; }

    /// <summary>
    /// Policy rule name.
    /// </summary>
    public required string PolicyName { get; init; }

    /// <summary>
    /// Whether the policy passed validation.
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// Explanation of the result.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Severity level if validation failed.
    /// </summary>
    public string? Severity { get; init; }
}

/// <summary>
/// A single step in the execution plan.
/// </summary>
public sealed class ExecutionStep
{
    /// <summary>
    /// Step number (1-based).
    /// </summary>
    public required int StepNumber { get; init; }

    /// <summary>
    /// Step description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Tool/operation to be invoked.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Whether this step has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Result of the step execution.
    /// </summary>
    public string? Result { get; set; }
}
