using CRISP.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CRISP.Audit;

/// <summary>
/// Extension methods for registering audit services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds CRISP audit logging services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddCrispAudit(this IServiceCollection services)
    {
        services.AddSingleton<IAuditLogger, AuditLogger>();
        return services;
    }
}
