namespace CRISP.Enterprise;

/// <summary>
/// Common interface for all CRISP enterprise modules.
/// Each module generates files and/or configures SCM settings
/// as part of the scaffolding process.
/// </summary>
public interface IEnterpriseModule
{
    /// <summary>
    /// Module identifier, e.g. "security-baseline", "sbom".
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name for plan display.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Execution order (lower = earlier). Security=100, SBOM=200, etc.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Whether this module should run given the current project context.
    /// For example, ApiContract only runs for API projects.
    /// </summary>
    bool ShouldRun(ProjectContext context);

    /// <summary>
    /// Execute the module: generate files, modify pipeline, configure SCM.
    /// </summary>
    Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default);
}
