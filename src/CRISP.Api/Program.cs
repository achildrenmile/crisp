using CRISP.Agent;
using CRISP.Api.Endpoints;
using CRISP.Api.Services;
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
using Microsoft.Extensions.Options;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/crisp-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting CRISP API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Configure settings
    builder.Services.Configure<CrispConfiguration>(
        builder.Configuration.GetSection("Crisp"));
    builder.Services.Configure<ClaudeApiOptions>(
        builder.Configuration.GetSection("Claude"));

    // Add HTTP client factory
    builder.Services.AddHttpClient("AzureDevOps");

    // Session management
    builder.Services.AddSingleton<ISessionManager, InMemorySessionManager>();

    // Claude client
    builder.Services.AddSingleton<IClaudeClient, ClaudeClient>();

    // Chat agent
    builder.Services.AddScoped<IChatAgent, ChatAgent>();

    // Register core CRISP services
    builder.Services.AddCrispAudit();
    builder.Services.AddCrispGit();
    builder.Services.AddCrispTemplates();
    builder.Services.AddCrispPipelines();

    // Register source control provider based on configuration
    builder.Services.AddSingleton<ISourceControlProvider>(sp =>
    {
        var config = sp.GetRequiredService<IOptions<CrispConfiguration>>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        if (config.Value.ScmPlatform == ScmPlatform.GitHub)
        {
            var logger = loggerFactory.CreateLogger<GitHubSourceControlProvider>();
            return new GitHubSourceControlProvider(logger, config);
        }
        else
        {
            var logger = loggerFactory.CreateLogger<AzureDevOpsSourceControlProvider>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new AzureDevOpsSourceControlProvider(logger, config, httpClientFactory);
        }
    });

    // Register policy engine
    builder.Services.AddSingleton<IPolicyEngine, PolicyEngineService>();

    // Register main CRISP agent
    builder.Services.AddScoped<ICrispAgent, CrispAgent>();

    // Add Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "CRISP API",
            Version = "v1",
            Description = "Code Repo Initialization & Scaffolding Platform API"
        });
    });

    // CORS for development
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Configure middleware
    app.UseCors();
    app.UseSerilogRequestLogging();

    // Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CRISP API v1");
        c.RoutePrefix = "swagger";
    });

    // Map endpoints
    app.MapChatEndpoints();
    app.MapHealthEndpoints();

    // Root redirect to Swagger
    app.MapGet("/", () => Results.Redirect("/swagger"));

    Log.Information("CRISP API started. Open http://localhost:5000/swagger for API documentation.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CRISP API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
