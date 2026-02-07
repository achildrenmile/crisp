using CRISP.Enterprise.ApiContract;
using CRISP.Enterprise.Branching;
using CRISP.Enterprise.Environment;
using CRISP.Enterprise.License;
using CRISP.Enterprise.Observability;
using CRISP.Enterprise.Ownership;
using CRISP.Enterprise.Readme;
using CRISP.Enterprise.Runbook;
using CRISP.Enterprise.Sbom;
using CRISP.Enterprise.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRISP.Enterprise;

/// <summary>
/// Extension methods for registering enterprise module services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CRISP Enterprise module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCrispEnterprise(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<EnterpriseConfiguration>(
            configuration.GetSection(EnterpriseConfiguration.SectionName));

        // Register orchestrator
        services.AddScoped<EnterpriseModuleOrchestrator>();

        // Register all modules
        services.AddScoped<IEnterpriseModule, SecurityBaselineModule>();
        services.AddScoped<IEnterpriseModule, SbomModule>();
        services.AddScoped<IEnterpriseModule, LicenseComplianceModule>();
        services.AddScoped<IEnterpriseModule, OwnershipModule>();
        services.AddScoped<IEnterpriseModule, BranchingStrategyModule>();
        services.AddScoped<IEnterpriseModule, ObservabilityModule>();
        services.AddScoped<IEnterpriseModule, ReadmeGeneratorModule>();
        services.AddScoped<IEnterpriseModule, EnvironmentConfigModule>();
        services.AddScoped<IEnterpriseModule, ApiContractModule>();
        services.AddScoped<IEnterpriseModule, RunbookModule>();

        return services;
    }
}
