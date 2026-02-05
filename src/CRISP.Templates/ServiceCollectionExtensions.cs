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
        // .NET
        services.AddSingleton<IProjectGenerator, AspNetCoreWebApiGenerator>();

        // Python
        services.AddSingleton<IProjectGenerator, FastApiGenerator>();
        services.AddSingleton<IProjectGenerator, FlaskGenerator>();
        services.AddSingleton<IProjectGenerator, DjangoGenerator>();

        // JavaScript/TypeScript
        services.AddSingleton<IProjectGenerator, ExpressGenerator>();
        services.AddSingleton<IProjectGenerator, ReactGenerator>();
        services.AddSingleton<IProjectGenerator, NextJsGenerator>();

        // Java
        services.AddSingleton<IProjectGenerator, SpringBootGenerator>();

        // Go
        services.AddSingleton<IProjectGenerator, GinGenerator>();

        return services;
    }
}
