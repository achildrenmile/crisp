using CRISP.Audit;
using CRISP.Core.Interfaces;
using CRISP.Mcp.AuditLog.Tools;
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

    builder.Services.AddSingleton<IAuditLogger, AuditLogger>();

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(AuditLogTools).Assembly);

    builder.Services.AddSerilog();

    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Audit Log MCP server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
