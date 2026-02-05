using CRISP.Agent;
using CRISP.Audit;
using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Git;
using CRISP.Mcp.AzureDevOps;
using CRISP.Mcp.GitHub;
using CRISP.Mcp.PolicyEngine;
using CRISP.Pipelines;
using CRISP.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/crisp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting CRISP Agent");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure settings
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables(prefix: "CRISP_");

    builder.Services.Configure<CrispConfiguration>(
        builder.Configuration.GetSection("Crisp"));

    // Add Serilog
    builder.Services.AddSerilog();

    // Add HTTP client factory
    builder.Services.AddHttpClient("AzureDevOps");

    // Register core services
    builder.Services.AddCrispAudit();
    builder.Services.AddCrispGit();
    builder.Services.AddCrispTemplates();
    builder.Services.AddCrispPipelines();

    // Register source control provider based on configuration
    builder.Services.AddSingleton<ISourceControlProvider>(sp =>
    {
        var config = sp.GetRequiredService<IOptions<CrispConfiguration>>();
        var logger = sp.GetRequiredService<ILogger<GitHubSourceControlProvider>>();
        var azdoLogger = sp.GetRequiredService<ILogger<AzureDevOpsSourceControlProvider>>();

        return config.Value.ScmPlatform switch
        {
            ScmPlatform.GitHub => new GitHubSourceControlProvider(logger, config),
            ScmPlatform.AzureDevOps => new AzureDevOpsSourceControlProvider(
                azdoLogger, config, sp.GetRequiredService<IHttpClientFactory>()),
            _ => throw new InvalidOperationException($"Unsupported SCM platform: {config.Value.ScmPlatform}")
        };
    });

    // Register policy engine
    builder.Services.AddSingleton<IPolicyEngine, PolicyEngineService>();

    // Register main agent
    builder.Services.AddSingleton<ICrispAgent, CrispAgent>();

    var app = builder.Build();

    // Run the agent
    var agent = app.Services.GetRequiredService<ICrispAgent>();

    // Validate configuration
    var validationErrors = await agent.ValidateConfigurationAsync();
    if (validationErrors.Count > 0)
    {
        Log.Warning("Configuration validation warnings:");
        foreach (var error in validationErrors)
        {
            Log.Warning("  - {Error}", error);
        }
    }

    Log.Information("CRISP Agent started. Session ID: {SessionId}", agent.SessionId);
    Log.Information("Ready to scaffold projects. Configure via appsettings.json or environment variables.");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CRISP Agent terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
