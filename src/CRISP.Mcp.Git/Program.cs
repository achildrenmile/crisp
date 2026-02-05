using CRISP.Core.Interfaces;
using CRISP.Git;
using CRISP.Mcp.Git.Tools;
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

    builder.Services.AddSingleton<IGitOperations, GitOperations>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(GitTools).Assembly);

    builder.Services.AddSerilog();

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Git MCP server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
