using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace CRISP.Enterprise;

/// <summary>
/// Runs all enabled enterprise modules in the correct order.
/// Called by CrispAgent after core scaffolding completes and before commit.
/// </summary>
public sealed class EnterpriseModuleOrchestrator
{
    private readonly IEnumerable<IEnterpriseModule> _modules;
    private readonly EnterpriseConfiguration _config;
    private readonly ILogger<EnterpriseModuleOrchestrator> _logger;

    public EnterpriseModuleOrchestrator(
        IEnumerable<IEnterpriseModule> modules,
        IOptions<EnterpriseConfiguration> config,
        ILogger<EnterpriseModuleOrchestrator> logger)
    {
        _modules = modules;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Execute all applicable modules in order.
    /// Each module receives the same ProjectContext and can see files created by prior modules.
    /// </summary>
    /// <param name="context">Project context with all scaffolding information.</param>
    /// <param name="onProgress">Optional callback for UI updates: (moduleId, status).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results from all executed modules.</returns>
    public async Task<IReadOnlyList<ModuleResult>> ExecuteAllAsync(
        ProjectContext context,
        Action<string, string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ModuleResult>();

        var enabledModules = _modules
            .Where(m => IsModuleEnabled(m.Id))
            .Where(m => m.ShouldRun(context))
            .OrderBy(m => m.Order)
            .ToList();

        _logger.LogInformation(
            "Executing {Count} enterprise modules for project {ProjectName}",
            enabledModules.Count,
            context.ProjectName);

        foreach (var module in enabledModules)
        {
            _logger.LogInformation(
                "Running enterprise module: {ModuleId} ({DisplayName})",
                module.Id,
                module.DisplayName);

            onProgress?.Invoke(module.Id, "running");
            var sw = Stopwatch.StartNew();

            try
            {
                var result = await module.ExecuteAsync(context, cancellationToken);
                result = result with { Duration = sw.Elapsed };
                results.Add(result);

                // Update context with newly created files so downstream modules can see them
                context.GeneratedFiles.AddRange(result.FilesCreated);

                _logger.LogInformation(
                    "Module {ModuleId} completed: {Success}, files created: {FileCount}",
                    module.Id,
                    result.Success ? "success" : "failed",
                    result.FilesCreated.Count);

                onProgress?.Invoke(module.Id, result.Success ? "completed" : "failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enterprise module {ModuleId} failed", module.Id);

                results.Add(new ModuleResult
                {
                    ModuleId = module.Id,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = sw.Elapsed
                });

                onProgress?.Invoke(module.Id, "failed");
                // Continue with remaining modules — one failure shouldn't block the rest
            }
        }

        _logger.LogInformation(
            "Enterprise modules completed: {SuccessCount}/{TotalCount} succeeded",
            results.Count(r => r.Success),
            results.Count);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets a list of modules that would run for the given context.
    /// Useful for plan display.
    /// </summary>
    public IReadOnlyList<(string Id, string DisplayName)> GetApplicableModules(ProjectContext context)
    {
        return _modules
            .Where(m => IsModuleEnabled(m.Id))
            .Where(m => m.ShouldRun(context))
            .OrderBy(m => m.Order)
            .Select(m => (m.Id, m.DisplayName))
            .ToList()
            .AsReadOnly();
    }

    private bool IsModuleEnabled(string moduleId) =>
        !_config.DisabledModules.Contains(moduleId, StringComparer.OrdinalIgnoreCase);
}
