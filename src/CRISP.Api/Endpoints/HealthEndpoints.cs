using CRISP.Api.Services;
using CRISP.Core.Interfaces;

namespace CRISP.Api.Endpoints;

/// <summary>
/// Health check endpoints.
/// </summary>
public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Health");

        group.MapGet("/health", async (
            ICrispAgent agent,
            ILlmClient llmClient,
            CancellationToken cancellationToken) =>
        {
            var errors = await agent.ValidateConfigurationAsync(cancellationToken);
            var llmInfo = llmClient.GetInfo();

            var health = new
            {
                status = errors.Count == 0 ? "healthy" : "degraded",
                timestamp = DateTime.UtcNow,
                llm = new
                {
                    provider = llmInfo.Provider,
                    model = llmInfo.Model,
                    baseUrl = llmInfo.BaseUrl
                },
                checks = new
                {
                    configuration = errors.Count == 0
                        ? "ok"
                        : string.Join("; ", errors)
                }
            };

            return errors.Count == 0
                ? Results.Ok(health)
                : Results.Json(health, statusCode: 503);
        })
        .WithName("HealthCheck")
        .WithDescription("Health check for all dependent services");

        group.MapGet("/llm-info", (ILlmClient llmClient) =>
        {
            var info = llmClient.GetInfo();
            return Results.Ok(new
            {
                provider = info.Provider,
                model = info.Model,
                baseUrl = info.BaseUrl
            });
        })
        .WithName("GetLlmInfo")
        .WithDescription("Get information about the configured LLM provider");
    }
}
