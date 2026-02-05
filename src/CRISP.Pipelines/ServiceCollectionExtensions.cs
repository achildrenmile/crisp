using CRISP.Core.Interfaces;
using CRISP.Pipelines.Generators;
using Microsoft.Extensions.DependencyInjection;

namespace CRISP.Pipelines;

/// <summary>
/// Extension methods for registering pipeline services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CRISP pipeline generation services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddCrispPipelines(this IServiceCollection services)
    {
        services.AddSingleton<IPipelineGenerator, GitHubActionsGenerator>();
        services.AddSingleton<IPipelineGenerator, AzurePipelinesGenerator>();

        return services;
    }
}
