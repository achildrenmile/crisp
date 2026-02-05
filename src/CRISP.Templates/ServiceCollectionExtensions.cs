using CRISP.Core.Interfaces;
using CRISP.Templates.Generators;
using Microsoft.Extensions.DependencyInjection;

namespace CRISP.Templates;

/// <summary>
/// Extension methods for registering template services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CRISP template engine services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddCrispTemplates(this IServiceCollection services)
    {
        // Register filesystem operations
        services.AddSingleton<IFilesystemOperations, FilesystemOperations>();

        // Register template engine
        services.AddSingleton<ITemplateEngine, ScribanTemplateEngine>();

        // Register built-in generators
        services.AddSingleton<IProjectGenerator, AspNetCoreWebApiGenerator>();
        services.AddSingleton<IProjectGenerator, FastApiGenerator>();

        return services;
    }
}
