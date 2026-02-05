using CRISP.Core.Configuration;
using CRISP.Core.Interfaces;
using CRISP.Mcp.GitHub;
using CRISP.Mcp.GitHub.Tools;
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

    // Register services
    builder.Services.AddSingleton<ISourceControlProvider, GitHubSourceControlProvider>();

    // Register MCP server with tools
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(GitHubTools).Assembly);

    builder.Services.AddSerilog();

    var app = builder.Build();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "GitHub MCP server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
