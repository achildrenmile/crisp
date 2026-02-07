using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.Observability;

/// <summary>
/// Scaffolds structured logging, health check endpoints, and tracing/metrics configuration
/// so the service is observable from the first deployment.
/// </summary>
public sealed class ObservabilityModule : IEnterpriseModule
{
    private readonly ILogger<ObservabilityModule> _logger;

    public ObservabilityModule(ILogger<ObservabilityModule> logger)
    {
        _logger = logger;
    }

    public string Id => "observability";
    public string DisplayName => "Observability Bootstrap";
    public int Order => 600;

    public bool ShouldRun(ProjectContext context) => true;

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        var filesCreated = new List<string>();

        try
        {
            // Generate observability documentation
            var docsDir = Path.Combine(context.WorkspacePath, "docs");
            Directory.CreateDirectory(docsDir);

            var observabilityDocPath = Path.Combine(docsDir, "observability.md");
            var observabilityDocContent = GenerateObservabilityDoc(context);
            await File.WriteAllTextAsync(observabilityDocPath, observabilityDocContent, cancellationToken);
            filesCreated.Add("docs/observability.md");

            // Generate language-specific observability code
            var scaffoldedFiles = await ScaffoldObservabilityCodeAsync(context, cancellationToken);
            filesCreated.AddRange(scaffoldedFiles);

            // Record ADR
            context.DecisionCollector.Record(
                title: $"Bootstrap observability with {context.ObservabilityProvider}",
                context: "Production services need observability from day one: structured logging, health checks, and distributed tracing.",
                decision: $"Configure {context.ObservabilityProvider} with structured JSON logging, /healthz and /ready endpoints, and distributed tracing.",
                rationale: "Having observability built in from the start prevents debugging production issues without visibility. OpenTelemetry provides vendor-neutral instrumentation.",
                category: AdrCategory.Infrastructure,
                alternatives: new Dictionary<string, string>
                {
                    ["OpenTelemetry"] = context.ObservabilityProvider == "opentelemetry"
                        ? "Selected for vendor-neutral, standards-based observability"
                        : "Vendor-neutral but requires more setup",
                    ["Application Insights"] = context.ObservabilityProvider == "applicationinsights"
                        ? "Selected for Azure ecosystem integration"
                        : "Azure-specific, less portable",
                    ["Datadog"] = context.ObservabilityProvider == "datadog"
                        ? "Selected for full-stack observability platform"
                        : "Proprietary but feature-rich",
                    ["None"] = "Would leave the service unobservable in production"
                },
                consequences: [
                    "All logs are structured JSON with correlation IDs",
                    "Health checks at /healthz (liveness) and /ready (readiness)",
                    "Distributed traces connect requests across services",
                    "Metrics available for dashboards and alerting"
                ],
                relatedFiles: filesCreated
            );

            return new ModuleResult
            {
                ModuleId = Id,
                Success = true,
                FilesCreated = filesCreated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Observability module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GenerateObservabilityDoc(ProjectContext context)
    {
        var provider = context.ObservabilityProvider.ToLowerInvariant() switch
        {
            "applicationinsights" => "Application Insights",
            "datadog" => "Datadog",
            _ => "OpenTelemetry"
        };

        return $"""
            # Observability

            This service is configured for production observability with structured logging,
            health checks, and distributed tracing.

            ## Health Checks

            | Endpoint | Purpose | Expected Response |
            |----------|---------|-------------------|
            | `GET /healthz` | Liveness probe | `200 OK` if process is alive |
            | `GET /ready` | Readiness probe | `200 OK` if dependencies are reachable |

            ### Kubernetes Integration

            ```yaml
            livenessProbe:
              httpGet:
                path: /healthz
                port: {context.Port}
              initialDelaySeconds: 5
              periodSeconds: 10

            readinessProbe:
              httpGet:
                path: /ready
                port: {context.Port}
              initialDelaySeconds: 5
              periodSeconds: 5
            ```

            ## Structured Logging

            All logs are emitted as structured JSON. Key fields:

            | Field | Description |
            |-------|-------------|
            | `timestamp` | ISO 8601 timestamp |
            | `level` | Log level: debug, info, warn, error |
            | `message` | Human-readable description |
            | `traceId` | Distributed trace correlation ID |
            | `spanId` | Current span ID |
            | `service` | Service name: `{context.ProjectName}` |

            ### Example Log Entry

            ```json
            {{
              "timestamp": "2024-01-15T10:30:00.000Z",
              "level": "info",
              "message": "Request completed",
              "traceId": "abc123",
              "spanId": "def456",
              "service": "{context.ProjectName}",
              "path": "/api/orders",
              "statusCode": 200,
              "durationMs": 45
            }}
            ```

            ## Tracing & Metrics

            - **Provider:** {provider}
            - **Exporter:** Console (dev), OTLP (prod)

            ### Configuration

            Configure the OTLP endpoint via environment variables:

            ```bash
            # OpenTelemetry Protocol endpoint
            OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317

            # Service identification
            OTEL_SERVICE_NAME={context.ProjectName}
            OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production
            ```

            ### Instrumented Operations

            - HTTP requests (incoming and outgoing)
            - Database queries{(context.HasDatabase ? $" ({context.DatabaseType})" : "")}
            - External API calls
            - Background jobs

            ## Recommended Alerts

            | Alert | Condition | Severity |
            |-------|-----------|----------|
            | Health check failing | `/healthz` returns non-200 for > 1 min | Critical |
            | High error rate | > 5% 5xx responses in 5 min | High |
            | High latency | p95 latency > 2s for 5 min | Medium |
            | Memory pressure | Memory > 85% for 10 min | Medium |

            ## Dashboards

            Recommended panels for a service dashboard:

            1. Request rate (RPM)
            2. Error rate (%)
            3. Latency distribution (p50, p95, p99)
            4. Active connections
            5. Health check status
            6. Resource usage (CPU, memory)

            ---

            *Observability configured by [CRISP](https://github.com/strali/crisp).*
            """;
    }

    private async Task<List<string>> ScaffoldObservabilityCodeAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        switch (context.Language.ToLowerInvariant())
        {
            case "csharp":
                files.AddRange(await ScaffoldDotNetObservabilityAsync(context, cancellationToken));
                break;
            case "python":
                files.AddRange(await ScaffoldPythonObservabilityAsync(context, cancellationToken));
                break;
            case "typescript":
            case "javascript":
                files.AddRange(await ScaffoldNodeObservabilityAsync(context, cancellationToken));
                break;
            case "dart":
                // Dart has limited OpenTelemetry support, just add health endpoints
                files.AddRange(await ScaffoldDartObservabilityAsync(context, cancellationToken));
                break;
        }

        return files;
    }

    private static async Task<List<string>> ScaffoldDotNetObservabilityAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        // Create Extensions directory
        var extensionsDir = Path.Combine(context.WorkspacePath, "src", context.ProjectName, "Extensions");
        if (!Directory.Exists(extensionsDir))
        {
            extensionsDir = Path.Combine(context.WorkspacePath, "Extensions");
        }
        Directory.CreateDirectory(extensionsDir);

        var observabilityExtPath = Path.Combine(extensionsDir, "ObservabilityExtensions.cs");
        var content = $$"""
            using Microsoft.AspNetCore.Diagnostics.HealthChecks;
            using Microsoft.Extensions.Diagnostics.HealthChecks;
            using System.Text.Json;

            namespace {{context.ProjectName.Replace("-", "")}}.Extensions;

            /// <summary>
            /// Extension methods for configuring observability (logging, health checks, tracing).
            /// </summary>
            public static class ObservabilityExtensions
            {
                /// <summary>
                /// Adds observability services including health checks and OpenTelemetry.
                /// </summary>
                public static IServiceCollection AddObservability(
                    this IServiceCollection services,
                    IConfiguration configuration)
                {
                    // Health checks
                    services.AddHealthChecks();
                    // TODO: Add database health check if using EF Core:
                    // .AddDbContextCheck<AppDbContext>();

                    // OpenTelemetry (uncomment when packages are added)
                    // services.AddOpenTelemetry()
                    //     .WithTracing(builder => builder
                    //         .AddAspNetCoreInstrumentation()
                    //         .AddHttpClientInstrumentation()
                    //         .AddOtlpExporter())
                    //     .WithMetrics(builder => builder
                    //         .AddAspNetCoreInstrumentation()
                    //         .AddHttpClientInstrumentation());

                    return services;
                }

                /// <summary>
                /// Maps observability endpoints (health checks).
                /// </summary>
                public static WebApplication MapObservability(this WebApplication app)
                {
                    // Liveness probe - is the process alive?
                    app.MapHealthChecks("/healthz", new HealthCheckOptions
                    {
                        Predicate = _ => false, // No dependency checks
                        ResponseWriter = WriteHealthResponse
                    });

                    // Readiness probe - is the service ready to accept traffic?
                    app.MapHealthChecks("/ready", new HealthCheckOptions
                    {
                        ResponseWriter = WriteHealthResponse
                    });

                    return app;
                }

                private static async Task WriteHealthResponse(HttpContext context, HealthReport report)
                {
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description,
                            duration = e.Value.Duration.TotalMilliseconds
                        })
                    };

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }));
                }
            }
            """;

        await File.WriteAllTextAsync(observabilityExtPath, content, cancellationToken);
        files.Add(Path.GetRelativePath(context.WorkspacePath, observabilityExtPath));

        return files;
    }

    private static async Task<List<string>> ScaffoldPythonObservabilityAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        // Create observability module
        var appDir = Path.Combine(context.WorkspacePath, "app");
        Directory.CreateDirectory(appDir);

        var observabilityPath = Path.Combine(appDir, "observability.py");
        var content = $$"""
            """Observability configuration: structured logging, tracing, health checks."""
            import logging
            import sys
            from datetime import datetime

            try:
                import structlog
                HAS_STRUCTLOG = True
            except ImportError:
                HAS_STRUCTLOG = False

            try:
                from opentelemetry import trace
                from opentelemetry.sdk.trace import TracerProvider
                from opentelemetry.sdk.trace.export import ConsoleSpanExporter, SimpleSpanProcessor
                HAS_OTEL = True
            except ImportError:
                HAS_OTEL = False


            def setup_logging():
                """Configure structured logging."""
                if HAS_STRUCTLOG:
                    structlog.configure(
                        processors=[
                            structlog.stdlib.filter_by_level,
                            structlog.stdlib.add_logger_name,
                            structlog.stdlib.add_log_level,
                            structlog.processors.TimeStamper(fmt="iso"),
                            structlog.processors.StackInfoRenderer(),
                            structlog.processors.format_exc_info,
                            structlog.processors.JSONRenderer(),
                        ],
                        context_class=dict,
                        logger_factory=structlog.stdlib.LoggerFactory(),
                        wrapper_class=structlog.stdlib.BoundLogger,
                        cache_logger_on_first_use=True,
                    )
                else:
                    # Fallback to standard logging with JSON-ish format
                    logging.basicConfig(
                        level=logging.INFO,
                        format='{"timestamp": "%(asctime)s", "level": "%(levelname)s", "message": "%(message)s"}',
                        stream=sys.stdout,
                    )


            def setup_tracing(app=None):
                """Configure OpenTelemetry tracing."""
                if not HAS_OTEL:
                    return

                provider = TracerProvider()
                processor = SimpleSpanProcessor(ConsoleSpanExporter())
                provider.add_span_processor(processor)
                trace.set_tracer_provider(provider)

                if app is not None:
                    try:
                        from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
                        FastAPIInstrumentor.instrument_app(app)
                    except ImportError:
                        pass


            def get_logger(name: str):
                """Get a configured logger."""
                if HAS_STRUCTLOG:
                    return structlog.get_logger(name)
                return logging.getLogger(name)
            """;

        await File.WriteAllTextAsync(observabilityPath, content, cancellationToken);
        files.Add("app/observability.py");

        // Create health routes
        var routesDir = Path.Combine(appDir, "routes");
        Directory.CreateDirectory(routesDir);

        var healthPath = Path.Combine(routesDir, "health.py");
        var healthContent = $$"""
            """Health check endpoints for Kubernetes probes."""
            from fastapi import APIRouter, Response

            router = APIRouter(tags=["health"])


            @router.get("/healthz")
            async def liveness():
                """Liveness probe - is the process alive?"""
                return {"status": "healthy"}


            @router.get("/ready")
            async def readiness():
                """Readiness probe - is the service ready to accept traffic?"""
                # TODO: Add dependency checks (database, cache, etc.)
                # Example:
                # try:
                #     await database.execute("SELECT 1")
                # except Exception:
                #     return Response(status_code=503, content='{"status": "not ready"}')

                return {"status": "ready"}
            """;

        await File.WriteAllTextAsync(healthPath, healthContent, cancellationToken);
        files.Add("app/routes/health.py");

        return files;
    }

    private static async Task<List<string>> ScaffoldNodeObservabilityAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        var srcDir = Path.Combine(context.WorkspacePath, "src");
        Directory.CreateDirectory(srcDir);

        var observabilityPath = Path.Combine(srcDir, "observability.ts");
        var content = """
            /**
             * Observability configuration: structured logging, tracing, health checks.
             */

            // Structured logging with pino (if installed)
            let logger: any;
            try {
              const pino = require('pino');
              logger = pino({
                level: process.env.LOG_LEVEL || 'info',
                formatters: {
                  level: (label: string) => ({ level: label }),
                },
                timestamp: () => `,"timestamp":"${new Date().toISOString()}"`,
              });
            } catch {
              // Fallback to console
              logger = {
                info: console.log,
                warn: console.warn,
                error: console.error,
                debug: console.debug,
              };
            }

            export { logger };

            // Health check handlers
            export const healthCheck = (_req: any, res: any) => {
              res.json({ status: 'healthy' });
            };

            export const readinessCheck = async (_req: any, res: any) => {
              // TODO: Add dependency checks
              // Example:
              // try {
              //   await db.query('SELECT 1');
              // } catch {
              //   return res.status(503).json({ status: 'not ready' });
              // }

              res.json({ status: 'ready' });
            };

            // OpenTelemetry setup (if packages installed)
            export const setupTracing = () => {
              try {
                const { NodeSDK } = require('@opentelemetry/sdk-node');
                const { getNodeAutoInstrumentations } = require('@opentelemetry/auto-instrumentations-node');

                const sdk = new NodeSDK({
                  instrumentations: [getNodeAutoInstrumentations()],
                });

                sdk.start();
                logger.info('OpenTelemetry tracing initialized');
              } catch {
                logger.warn('OpenTelemetry not configured - install @opentelemetry/sdk-node');
              }
            };
            """;

        await File.WriteAllTextAsync(observabilityPath, content, cancellationToken);
        files.Add("src/observability.ts");

        return files;
    }

    private static async Task<List<string>> ScaffoldDartObservabilityAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        var libDir = Path.Combine(context.WorkspacePath, "lib", "src");
        Directory.CreateDirectory(libDir);

        var observabilityPath = Path.Combine(libDir, "observability.dart");
        var content = """
            /// Observability utilities for structured logging and health checks.
            library;

            import 'dart:convert';
            import 'dart:io';

            /// Structured logger that outputs JSON.
            class Logger {
              final String name;

              Logger(this.name);

              void info(String message, [Map<String, dynamic>? data]) =>
                  _log('info', message, data);

              void warn(String message, [Map<String, dynamic>? data]) =>
                  _log('warn', message, data);

              void error(String message, [Map<String, dynamic>? data]) =>
                  _log('error', message, data);

              void _log(String level, String message, Map<String, dynamic>? data) {
                final entry = {
                  'timestamp': DateTime.now().toUtc().toIso8601String(),
                  'level': level,
                  'logger': name,
                  'message': message,
                  ...?data,
                };
                stdout.writeln(jsonEncode(entry));
              }
            }

            /// Health check response.
            class HealthStatus {
              final String status;
              final Map<String, dynamic>? checks;

              HealthStatus({required this.status, this.checks});

              Map<String, dynamic> toJson() => {
                    'status': status,
                    if (checks != null) 'checks': checks,
                  };
            }

            /// Performs health checks.
            Future<HealthStatus> checkHealth() async {
              // TODO: Add dependency checks
              return HealthStatus(status: 'healthy');
            }

            /// Performs readiness checks.
            Future<HealthStatus> checkReadiness() async {
              // TODO: Add dependency checks (database, external services)
              return HealthStatus(status: 'ready');
            }
            """;

        await File.WriteAllTextAsync(observabilityPath, content, cancellationToken);
        files.Add("lib/src/observability.dart");

        return files;
    }
}
