namespace CRISP.Enterprise;

/// <summary>
/// Result of executing an enterprise module.
/// </summary>
public sealed record ModuleResult
{
    /// <summary>
    /// Module identifier.
    /// </summary>
    public required string ModuleId { get; init; }

    /// <summary>
    /// Whether the module executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Files created by this module.
    /// </summary>
    public List<string> FilesCreated { get; init; } = [];

    /// <summary>
    /// Files modified by this module.
    /// </summary>
    public List<string> FilesModified { get; init; } = [];

    /// <summary>
    /// Pipeline steps added by this module.
    /// </summary>
    public List<string> PipelineStepsAdded { get; init; } = [];

    /// <summary>
    /// SCM configuration actions taken: "branch-protection:main", "codeowners:created".
    /// </summary>
    public List<string> ScmConfigActions { get; init; } = [];

    /// <summary>
    /// Error message if the module failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Duration of module execution.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ModuleResult Succeeded(string moduleId) => new()
    {
        ModuleId = moduleId,
        Success = true
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ModuleResult Failed(string moduleId, string errorMessage) => new()
    {
        ModuleId = moduleId,
        Success = false,
        ErrorMessage = errorMessage
    };
}
