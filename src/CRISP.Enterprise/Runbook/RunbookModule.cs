using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.Runbook;

/// <summary>
/// Generates an operations runbook template with deployment procedures,
/// incident response, and troubleshooting guidance.
/// </summary>
public sealed class RunbookModule : IEnterpriseModule
{
    private readonly ILogger<RunbookModule> _logger;

    public RunbookModule(ILogger<RunbookModule> logger)
    {
        _logger = logger;
    }

    public string Id => "runbook";
    public string DisplayName => "Operations Runbook";
    public int Order => 1000;

    public bool ShouldRun(ProjectContext context) => true;

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create docs directory
            var docsDir = Path.Combine(context.WorkspacePath, "docs");
            Directory.CreateDirectory(docsDir);

            // Generate runbook
            var runbookPath = Path.Combine(docsDir, "runbook.md");
            var runbookContent = GenerateRunbook(context);
            await File.WriteAllTextAsync(runbookPath, runbookContent, cancellationToken);

            // Record ADR
            context.DecisionCollector.Record(
                title: "Include operations runbook with deployment, rollback, incident response, and monitoring guidance",
                context: "Production services need documented procedures for deployment, troubleshooting, and incident response.",
                decision: "Generate an operations runbook with deployment steps, rollback procedures, incident severity levels, and troubleshooting guidance.",
                rationale: "Having runbook documentation from the start prevents scrambling during incidents. It captures operational knowledge and supports on-call engineers.",
                category: AdrCategory.Operations,
                consequences: [
                    "Deployment procedures are documented",
                    "Incident response process is clear",
                    "Troubleshooting steps are available for common issues",
                    "New team members can operate the service"
                ],
                relatedFiles: ["docs/runbook.md"]
            );

            return new ModuleResult
            {
                ModuleId = Id,
                Success = true,
                FilesCreated = ["docs/runbook.md"]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Runbook module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GenerateRunbook(ProjectContext context)
    {
        var deploymentSection = GenerateDeploymentSection(context);
        var rollbackSection = GenerateRollbackSection(context);
        var troubleshootingSection = GenerateTroubleshootingSection(context);

        return $"""
            # Operations Runbook — {context.ProjectName}

            > This runbook was auto-generated during project scaffolding.
            > Update it as your deployment and operational procedures evolve.

            ## Service Overview

            | Property | Value |
            |----------|-------|
            | Service Name | {context.ProjectName} |
            | Language/Runtime | {context.Runtime} |
            | Framework | {context.Framework} |
            | Repository | {context.RepositoryUrl} |
            | CI/CD | {context.CiPipelineFile ?? "Not configured"} |
            | Health Check | `GET /healthz` |
            | Readiness Check | `GET /ready` |
            | Default Port | {context.Port} |

            ## Deployment

            {deploymentSection}

            ## Rollback Procedure

            {rollbackSection}

            ## Incident Response

            ### Severity Levels

            | Level | Description | Response Time | Example |
            |-------|-------------|---------------|---------|
            | SEV-1 | Service completely down | Immediate (< 15 min) | Health check failing, no traffic served |
            | SEV-2 | Major functionality impaired | < 1 hour | API returning 500s on critical endpoints |
            | SEV-3 | Minor issue, workaround exists | < 4 hours | Non-critical endpoint slow |
            | SEV-4 | Cosmetic / low impact | Next sprint | Minor UI issue, typo |

            ### Incident Checklist

            - [ ] **Confirm** the issue (check health endpoints, logs, metrics)
            - [ ] **Assess** severity level
            - [ ] **Notify** stakeholders (Slack, PagerDuty, etc.)
            - [ ] **Investigate** (check recent deployments, error logs)
            - [ ] **Mitigate** (apply fix or rollback)
            - [ ] **Verify** resolution (health checks pass, errors stop)
            - [ ] **Communicate** resolution to stakeholders
            - [ ] **Document** post-mortem (for SEV-1 and SEV-2)

            ### Escalation Path

            1. On-call engineer
            2. Team lead / Tech lead
            3. Engineering manager
            4. VP Engineering (SEV-1 only)

            ## Troubleshooting

            {troubleshootingSection}

            ## Monitoring

            ### Health Endpoints

            | Endpoint | Purpose | Expected |
            |----------|---------|----------|
            | `GET /healthz` | Liveness probe | `200 OK` |
            | `GET /ready` | Readiness probe | `200 OK` |

            ### Key Metrics

            | Metric | Description | Alert Threshold |
            |--------|-------------|-----------------|
            | Request rate | Requests per second | N/A (baseline) |
            | Error rate | % of 5xx responses | > 5% for 5 min |
            | Latency (p95) | 95th percentile response time | > 2s for 5 min |
            | Memory usage | Container memory | > 85% for 10 min |
            | CPU usage | Container CPU | > 90% for 10 min |

            ### Recommended Alerts

            | Alert | Condition | Severity | Action |
            |-------|-----------|----------|--------|
            | Health check failing | `/healthz` non-200 for > 1 min | SEV-1 | Page on-call |
            | High error rate | > 5% 5xx in 5 min | SEV-2 | Page on-call |
            | High latency | p95 > 2s for 5 min | SEV-3 | Notify Slack |
            | Memory pressure | > 85% for 10 min | SEV-3 | Notify Slack |
            | Deployment failed | CI/CD pipeline failed | SEV-3 | Notify Slack |

            ### Log Queries

            **Find errors in the last hour:**
            ```
            level:error AND service:{context.ProjectName} AND @timestamp:[now-1h TO now]
            ```

            **Find slow requests:**
            ```
            service:{context.ProjectName} AND durationMs:>1000 AND @timestamp:[now-1h TO now]
            ```

            **Trace a specific request:**
            ```
            traceId:<trace-id>
            ```

            ## Contacts

            | Role | Contact |
            |------|---------|
            | Team | {context.TeamName ?? "TBD"} — {context.TeamEmail ?? "TBD"} |
            | On-call | *Configure in PagerDuty / Opsgenie* |
            | Security | See [SECURITY.md](../SECURITY.md) |

            ## Runbook Maintenance

            - Review this runbook quarterly
            - Update after every significant incident
            - Add new troubleshooting entries as issues are discovered
            - Keep contact information current

            ---

            *This runbook was generated by [CRISP](https://github.com/strali/crisp).*
            """;
    }

    private static string GenerateDeploymentSection(ProjectContext context)
    {
        var prerequisites = GetPrerequisites(context);
        var deploymentSteps = context.HasDocker
            ? GetDockerDeploymentSteps(context)
            : GetNativeDeploymentSteps(context);

        return $"""
            ### Prerequisites

            {prerequisites}

            ### Deployment Steps

            {deploymentSteps}

            ### Verification

            After deployment, verify the service is healthy:

            ```bash
            # Check health endpoint
            curl -f http://localhost:{context.Port}/healthz

            # Check readiness endpoint
            curl -f http://localhost:{context.Port}/ready

            # Check logs for errors
            docker logs {context.ProjectName} --tail 100 2>&1 | grep -i error
            ```
            """;
    }

    private static string GetPrerequisites(ProjectContext context)
    {
        var items = new List<string>
        {
            $"- {context.Runtime} (or Docker)"
        };

        if (context.HasDocker)
        {
            items.Add("- Docker 20.10+");
            items.Add("- Docker Compose (optional)");
        }

        if (context.HasDatabase)
        {
            items.Add($"- {context.DatabaseType} instance accessible from deployment target");
        }

        return string.Join("\n", items);
    }

    private static string GetDockerDeploymentSteps(ProjectContext context) => $$"""
        1. **Pull or build the Docker image:**
           ```bash
           # Option A: Pull from registry
           docker pull registry.example.com/{{context.ProjectName}}:latest

           # Option B: Build locally
           docker build -t {{context.ProjectName}}:latest .
           ```

        2. **Prepare environment:**
           ```bash
           # Create .env file from template
           cp .env.example .env
           # Edit .env with production values
           ```

        3. **Run the container:**
           ```bash
           docker run -d \
             --name {{context.ProjectName}} \
             -p {{context.Port}}:{{context.Port}} \
             --env-file .env \
             --restart unless-stopped \
             {{context.ProjectName}}:latest
           ```

        4. **Using Docker Compose (recommended):**
           ```bash
           docker-compose up -d
           ```

        5. **Verify health check:**
           ```bash
           curl http://localhost:{{context.Port}}/healthz
           # Expected: {"status": "healthy"}
           ```
        """;

    private static string GetNativeDeploymentSteps(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => $"""
            1. **Install dependencies:**
               ```bash
               dotnet restore
               ```

            2. **Build the application:**
               ```bash
               dotnet publish -c Release -o ./publish
               ```

            3. **Set environment variables:**
               ```bash
               export ASPNETCORE_ENVIRONMENT=Production
               export ASPNETCORE_URLS=http://+:{context.Port}
               # Set other required variables from .env.example
               ```

            4. **Run the application:**
               ```bash
               cd publish
               dotnet {context.ProjectName}.dll
               ```

            5. **Verify health check:**
               ```bash
               curl http://localhost:{context.Port}/healthz
               ```
            """,
        "python" => $"""
            1. **Create virtual environment:**
               ```bash
               python -m venv venv
               source venv/bin/activate
               ```

            2. **Install dependencies:**
               ```bash
               pip install -r requirements.txt
               ```

            3. **Set environment variables:**
               ```bash
               export APP_ENV=production
               # Set other required variables from .env.example
               ```

            4. **Run the application:**
               ```bash
               uvicorn app.main:app --host 0.0.0.0 --port {context.Port}
               ```

            5. **Verify health check:**
               ```bash
               curl http://localhost:{context.Port}/healthz
               ```
            """,
        _ => $"""
            1. **Install dependencies** according to project documentation

            2. **Set environment variables** from `.env.example`

            3. **Run the application** on port {context.Port}

            4. **Verify health check:**
               ```bash
               curl http://localhost:{context.Port}/healthz
               ```
            """
    };

    private static string GenerateRollbackSection(ProjectContext context)
    {
        if (context.HasDocker)
        {
            // Using $$ for interpolation so that Docker's Go template {.Tag} syntax is literal
            return $$"""
                ### Docker Rollback

                1. **Identify the last known good version:**
                   ```bash
                   docker images {{context.ProjectName}} --format "{.Tag}\t{.CreatedAt}"
                   ```

                2. **Stop the current container:**
                   ```bash
                   docker stop {{context.ProjectName}}
                   docker rm {{context.ProjectName}}
                   ```

                3. **Run the previous version:**
                   ```bash
                   docker run -d \
                     --name {{context.ProjectName}} \
                     -p {{context.Port}}:{{context.Port}} \
                     --env-file .env \
                     {{context.ProjectName}}:<previous-tag>
                   ```

                4. **Verify health check passes:**
                   ```bash
                   curl http://localhost:{{context.Port}}/healthz
                   ```

                5. **Investigate root cause** before re-deploying the failed version.
                """;
        }

        return """
            ### Rollback Procedure

            1. **Identify the last known good commit:**
               ```bash
               git log --oneline -10
               ```

            2. **Checkout the previous version:**
               ```bash
               git checkout <commit-hash>
               ```

            3. **Redeploy** using the standard deployment steps.

            4. **Verify health check passes.**

            5. **Investigate root cause** before re-deploying the failed version.
            """;
    }

    private static string GenerateTroubleshootingSection(ProjectContext context)
    {
        var dbTroubleshooting = context.HasDatabase ? $"""

            ### Database Connection Errors

            1. Check database is running and accessible
            2. Verify connection string in environment variables
            3. Check network/firewall rules
            4. Test connection manually:
               ```bash
               {GetDbTestCommand(context.DatabaseType)}
               ```
            """ : "";

        return $"""
            ### Service Won't Start

            1. **Check environment variables** are set correctly:
               ```bash
               # Compare with .env.example
               env | grep -E '^(APP_|DB_|OTEL_)'
               ```

            2. **Check logs** for startup errors:
               ```bash
               docker logs {context.ProjectName} --tail 100
               ```

            3. **Verify port {context.Port} is not in use:**
               ```bash
               lsof -i :{context.Port}
               ```

            4. **Check resource constraints** (memory, CPU)
            {dbTroubleshooting}

            ### High Error Rate

            1. **Check application logs** for error patterns:
               ```bash
               docker logs {context.ProjectName} 2>&1 | grep -i error | tail -50
               ```

            2. **Check dependency health** (database, external APIs)

            3. **Check recent deployments** — consider rollback

            4. **Check resource usage:**
               ```bash
               docker stats {context.ProjectName}
               ```

            ### High Latency

            1. **Check resource usage** (CPU, memory)

            2. **Check database query performance**

            3. **Check external API latencies**

            4. **Review recent code changes** for performance regressions

            5. **Scale horizontally** if resource-bound

            ### Memory Leak Suspected

            1. **Monitor memory over time:**
               ```bash
               docker stats {context.ProjectName} --no-stream
               ```

            2. **Check for gradual increase** without corresponding load increase

            3. **Restart as temporary mitigation**

            4. **Collect heap dump** for analysis (if applicable)
            """;
    }

    private static string GetDbTestCommand(string? dbType) => dbType?.ToLowerInvariant() switch
    {
        "postgresql" or "postgres" => "psql -h $DB_HOST -U $DB_USER -d $DB_NAME -c 'SELECT 1'",
        "mysql" or "mariadb" => "mysql -h $DB_HOST -u $DB_USER -p$DB_PASSWORD $DB_NAME -e 'SELECT 1'",
        "sqlserver" or "mssql" => "sqlcmd -S $DB_HOST -U $DB_USER -P $DB_PASSWORD -Q 'SELECT 1'",
        "mongodb" => "mongosh $DB_HOST/$DB_NAME --eval 'db.runCommand({ping:1})'",
        _ => "# Test database connection according to your database type"
    };
}
