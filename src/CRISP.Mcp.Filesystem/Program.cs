using CRISP.Core.Configuration;
using CRISP.Core.Interfaces;
using CRISP.Mcp.Filesystem.Tools;
using CRISP.Templates;
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

    builder.Services.Configure<CrispConfiguration>(
        builder.Configuration.GetSection("Crisp"));

    builder.Services.AddSingleton<IFilesystemOperations, FilesystemOperations>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(FilesystemTools).Assembly);

    builder.Services.AddSerilog();

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Filesystem MCP server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
