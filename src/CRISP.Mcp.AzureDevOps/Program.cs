using CRISP.Core.Configuration;
using CRISP.Core.Interfaces;
using CRISP.Mcp.AzureDevOps;
using CRISP.Mcp.AzureDevOps.Tools;
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

    // Configure CRISP settings
    builder.Services.Configure<CrispConfiguration>(
        builder.Configuration.GetSection("Crisp"));

    // Register HTTP client
    builder.Services.AddHttpClient("AzureDevOps");

    // Register services
    builder.Services.AddSingleton<ISourceControlProvider, AzureDevOpsSourceControlProvider>();

    // Register MCP server with tools
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(AzureDevOpsTools).Assembly);

    builder.Services.AddSerilog();

    var app = builder.Build();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Azure DevOps MCP server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
