using CRISP.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CRISP.Git;

/// <summary>
/// Extension methods for registering Git services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CRISP Git operation services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddCrispGit(this IServiceCollection services)
    {
        services.AddSingleton<IGitOperations, GitOperations>();
        return services;
    }
}
