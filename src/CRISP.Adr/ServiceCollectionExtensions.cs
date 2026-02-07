using CRISP.Adr.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRISP.Adr;

/// <summary>
/// Extension methods for registering ADR services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CRISP ADR module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind ADR options from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCrispAdr(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<AdrConfiguration>(
            configuration.GetSection(AdrConfiguration.SectionName));

        // Register services
        services.AddSingleton<AdrTemplateEngine>();
        services.AddSingleton<AdrIndexGenerator>();
        services.AddScoped<IAdrGenerator, AdrGenerator>();
        services.AddScoped<DecisionCollector>();

        return services;
    }

    /// <summary>
    /// Adds CRISP ADR module services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure ADR options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCrispAdr(
        this IServiceCollection services,
        Action<AdrConfiguration> configure)
    {
        // Configure with provided action
        services.Configure(configure);

        // Register services
        services.AddSingleton<AdrTemplateEngine>();
        services.AddSingleton<AdrIndexGenerator>();
        services.AddScoped<IAdrGenerator, AdrGenerator>();
        services.AddScoped<DecisionCollector>();

        return services;
    }
}
