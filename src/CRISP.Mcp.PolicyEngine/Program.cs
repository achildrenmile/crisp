using CRISP.Core.Interfaces;
using CRISP.Mcp.PolicyEngine;
using CRISP.Mcp.PolicyEngine.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSingleton<IPolicyEngine, PolicyEngineService>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(PolicyEngineTools).Assembly);

    builder.Services.AddSerilog();

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Policy Engine MCP server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
