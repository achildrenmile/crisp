namespace CRISP.Core.Models;

/// <summary>
/// Result of the scaffolding operation.
/// </summary>
public sealed class ScaffoldingResult
{
    /// <summary>
    /// Whether the scaffolding was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Path to the scaffolded project workspace.
    /// </summary>
    public required string WorkspacePath { get; init; }

    /// <summary>
    /// List of files created.
    /// </summary>
    public required IReadOnlyList<string> CreatedFiles { get; init; }

    /// <summary>
    /// Error message if scaffolding failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Warnings generated during scaffolding.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Final delivery result returned to the developer.
/// </summary>
public sealed class DeliveryResult
{
    /// <summary>
    /// Whether the entire operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Source control platform used.
    /// </summary>
    public required string Platform { get; init; }

    /// <summary>
    /// Repository URL.
    /// </summary>
    public required string RepositoryUrl { get; init; }

    /// <summary>
    /// Clone URL for the repository.
    /// </summary>
    public required string CloneUrl { get; init; }

    /// <summary>
    /// Default branch name.
    /// </summary>
    public required string DefaultBranch { get; init; }

    /// <summary>
    /// CI/CD pipeline URL (if applicable).
    /// </summary>
    public string? PipelineUrl { get; init; }

    /// <summary>
    /// Build status of the initial CI run.
    /// </summary>
    public string? BuildStatus { get; init; }

    /// <summary>
    /// VS Code web URL (vscode.dev) for instant browser editing.
    /// </summary>
    public required string VsCodeWebUrl { get; init; }

    /// <summary>
    /// VS Code clone URL (vscode:// protocol) for cloning to desktop.
    /// </summary>
    public required string VsCodeCloneUrl { get; init; }

    /// <summary>
    /// Collection URL (Azure DevOps only).
    /// </summary>
    public string? CollectionUrl { get; init; }

    /// <summary>
    /// Project name (Azure DevOps only).
    /// </summary>
    public string? ProjectName { get; init; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Formatted summary card for display.
    /// </summary>
    public required string SummaryCard { get; init; }
}
