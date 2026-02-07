using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.Environment;

/// <summary>
/// Scaffolds environment-specific configuration structure and secrets management integration.
/// </summary>
public sealed class EnvironmentConfigModule : IEnterpriseModule
{
    private readonly ILogger<EnvironmentConfigModule> _logger;

    public EnvironmentConfigModule(ILogger<EnvironmentConfigModule> logger)
    {
        _logger = logger;
    }

    public string Id => "environment-config";
    public string DisplayName => "Environment Configuration";
    public int Order => 800;

    public bool ShouldRun(ProjectContext context) => true;

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        var filesCreated = new List<string>();

        try
        {
            // Create docs directory
            var docsDir = Path.Combine(context.WorkspacePath, "docs");
            Directory.CreateDirectory(docsDir);

            // Generate environments documentation
            var envDocPath = Path.Combine(docsDir, "environments.md");
            var envDocContent = GenerateEnvironmentsDoc(context);
            await File.WriteAllTextAsync(envDocPath, envDocContent, cancellationToken);
            filesCreated.Add("docs/environments.md");

            // Generate language-specific config files
            var configFiles = await GenerateConfigFilesAsync(context, cancellationToken);
            filesCreated.AddRange(configFiles);

            // Record ADR
            var pattern = GetConfigPattern(context);
            var secretsInfo = context.SecretsManager != "none"
                ? $" with {FormatSecretsManager(context.SecretsManager)} integration"
                : " using environment variables";

            context.DecisionCollector.Record(
                title: $"Structure environment configuration with {pattern}{secretsInfo}",
                context: "Applications need clear separation between environment-specific settings and consistent configuration loading.",
                decision: $"Use {pattern} configuration hierarchy{secretsInfo} for managing environment-specific settings.",
                rationale: "Structured configuration prevents hardcoded values, simplifies deployments across environments, and provides a clear pattern for secrets management.",
                category: AdrCategory.Infrastructure,
                alternatives: new Dictionary<string, string>
                {
                    ["Environment variables only"] = "Simple but lacks structure for complex configuration",
                    ["Single config file"] = "Doesn't support environment-specific overrides",
                    ["Secrets manager"] = context.SecretsManager == "none"
                        ? "Not selected — using environment variables for simplicity"
                        : "Selected for secure secrets storage"
                },
                consequences: [
                    "Configuration is separated by environment",
                    "Sensitive values are not committed to source control",
                    $".env.example documents required variables"
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
            _logger.LogError(ex, "Environment config module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GenerateEnvironmentsDoc(ProjectContext context)
    {
        var configHierarchy = GetConfigHierarchyDoc(context);
        var secretsSection = GenerateSecretsSection(context);

        return $"""
            # Environment Configuration

            This document describes the environment configuration strategy for {context.ProjectName}.

            ## Environments

            | Environment | Purpose | Config Source |
            |-------------|---------|---------------|
            | `development` | Local development | `.env` file / local config |
            | `staging` | Pre-production testing | Environment variables / {FormatSecretsManager(context.SecretsManager)} |
            | `production` | Live | Environment variables / {FormatSecretsManager(context.SecretsManager)} |

            ## Configuration Hierarchy

            {configHierarchy}

            ## Required Variables

            See `.env.example` for the complete list of environment variables.

            ### Core Settings

            | Variable | Description | Required |
            |----------|-------------|----------|
            | `APP_ENV` | Environment name (development/staging/production) | Yes |
            | `APP_PORT` | HTTP port | Yes (default: {context.Port}) |
            {(context.HasDatabase ? GetDatabaseVarsTable(context) : "")}

            {secretsSection}

            ## Local Development

            1. Copy `.env.example` to `.env`:
               ```bash
               cp .env.example .env
               ```

            2. Fill in the required values

            3. Start the application (it will load from `.env` automatically)

            ## Staging/Production

            In staging and production:

            1. Set environment variables via your deployment platform
            2. {(context.SecretsManager != "none" ? $"Configure {FormatSecretsManager(context.SecretsManager)} for sensitive values" : "Use secure environment variable injection")}
            3. Never commit actual secrets to source control

            ---

            *This configuration was scaffolded by [CRISP](https://github.com/strali/crisp).*
            """;
    }

    private static string GetConfigHierarchyDoc(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => """
            Configuration is loaded in this order (later sources override earlier):

            1. `appsettings.json` — base configuration
            2. `appsettings.{Environment}.json` — environment-specific overrides
            3. Environment variables
            4. User secrets (development only)
            """,
        "python" => """
            Configuration is loaded in this order:

            1. Default values in code
            2. `.env` file (local development)
            3. Environment variables (override .env)
            """,
        "typescript" or "javascript" => """
            Configuration is loaded in this order:

            1. Default values in `config.ts`
            2. `.env` file (via dotenv)
            3. Environment variables (override .env)
            """,
        "java" => """
            Configuration is loaded in this order:

            1. `application.yml` — base configuration
            2. `application-{profile}.yml` — profile-specific overrides
            3. Environment variables
            """,
        _ => """
            Configuration is loaded from environment variables and config files.
            """
    };

    private static string GenerateSecretsSection(ProjectContext context)
    {
        if (context.SecretsManager == "none")
        {
            return """
                ## Secrets Management

                Secrets are managed via environment variables. For production:

                1. Use your deployment platform's secrets management
                2. Never log or expose secret values
                3. Rotate secrets regularly

                Consider adopting a secrets manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
                for improved security and audit trails.
                """;
        }

        return context.SecretsManager.ToLowerInvariant() switch
        {
            "azure-keyvault" => """
                ## Secrets Management — Azure Key Vault

                This project is pre-configured for Azure Key Vault integration.

                ### Setup

                1. Create a Key Vault in Azure Portal

                2. Add secrets to the vault:
                   ```bash
                   az keyvault secret set --vault-name <vault-name> --name "ConnectionStrings--Database" --value "<connection-string>"
                   ```

                3. Configure the application:
                   ```bash
                   AZURE_KEYVAULT_URL=https://<vault-name>.vault.azure.net/
                   ```

                4. Grant access to the application identity (Managed Identity or Service Principal)

                ### Local Development

                Use Azure CLI authentication:
                ```bash
                az login
                ```

                The application will use your Azure CLI credentials to access Key Vault.
                """,
            "aws-secrets" => """
                ## Secrets Management — AWS Secrets Manager

                This project is pre-configured for AWS Secrets Manager integration.

                ### Setup

                1. Create secrets in AWS Secrets Manager:
                   ```bash
                   aws secretsmanager create-secret --name "myapp/database" --secret-string '{"username":"user","password":"pass"}'
                   ```

                2. Configure the application:
                   ```bash
                   AWS_SECRETS_REGION=us-east-1
                   ```

                3. Grant IAM permissions to the application role

                ### Local Development

                Use AWS CLI credentials:
                ```bash
                aws configure
                ```
                """,
            "hashicorp-vault" => """
                ## Secrets Management — HashiCorp Vault

                This project is pre-configured for HashiCorp Vault integration.

                ### Setup

                1. Configure Vault address:
                   ```bash
                   VAULT_ADDR=https://vault.example.com
                   VAULT_TOKEN=<token>
                   ```

                2. Store secrets in Vault:
                   ```bash
                   vault kv put secret/myapp database_password=<password>
                   ```

                3. Configure AppRole or other authentication method for production

                ### Local Development

                Use development token:
                ```bash
                vault server -dev
                export VAULT_TOKEN=<dev-token>
                ```
                """,
            _ => ""
        };
    }

    private static string GetDatabaseVarsTable(ProjectContext context) => $"""
        | `DB_HOST` | Database host | Yes |
        | `DB_PORT` | Database port | Yes (default: {GetDefaultDbPort(context.DatabaseType)}) |
        | `DB_NAME` | Database name | Yes |
        | `DB_USER` | Database username | Yes |
        | `DB_PASSWORD` | Database password | Yes |
        """;

    private static string GetDefaultDbPort(string? dbType) => dbType?.ToLowerInvariant() switch
    {
        "postgresql" or "postgres" => "5432",
        "mysql" or "mariadb" => "3306",
        "sqlserver" or "mssql" => "1433",
        "mongodb" => "27017",
        _ => "5432"
    };

    private static string GetConfigPattern(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => "appsettings hierarchy",
        "python" => "Pydantic settings",
        "typescript" or "javascript" => "dotenv with typed config",
        "java" => "Spring profiles",
        _ => "environment-based"
    };

    private static string FormatSecretsManager(string secretsManager) => secretsManager.ToLowerInvariant() switch
    {
        "azure-keyvault" => "Azure Key Vault",
        "aws-secrets" => "AWS Secrets Manager",
        "hashicorp-vault" => "HashiCorp Vault",
        _ => "environment variables"
    };

    private async Task<List<string>> GenerateConfigFilesAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        switch (context.Language.ToLowerInvariant())
        {
            case "csharp":
                files.AddRange(await GenerateDotNetConfigAsync(context, cancellationToken));
                break;
            case "python":
                files.AddRange(await GeneratePythonConfigAsync(context, cancellationToken));
                break;
            case "typescript":
            case "javascript":
                files.AddRange(await GenerateNodeConfigAsync(context, cancellationToken));
                break;
        }

        return files;
    }

    private static async Task<List<string>> GenerateDotNetConfigAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        // Find the project directory
        var projectDir = Path.Combine(context.WorkspacePath, "src", context.ProjectName);
        if (!Directory.Exists(projectDir))
        {
            projectDir = context.WorkspacePath;
        }

        // Generate appsettings.Staging.json
        var stagingPath = Path.Combine(projectDir, "appsettings.Staging.json");
        if (!File.Exists(stagingPath))
        {
            var stagingContent = """
                {
                  "Logging": {
                    "LogLevel": {
                      "Default": "Information",
                      "Microsoft.AspNetCore": "Warning"
                    }
                  }
                }
                """;
            await File.WriteAllTextAsync(stagingPath, stagingContent, cancellationToken);
            files.Add(Path.GetRelativePath(context.WorkspacePath, stagingPath));
        }

        // Generate appsettings.Production.json
        var prodPath = Path.Combine(projectDir, "appsettings.Production.json");
        if (!File.Exists(prodPath))
        {
            var prodContent = """
                {
                  "Logging": {
                    "LogLevel": {
                      "Default": "Warning",
                      "Microsoft.AspNetCore": "Warning"
                    }
                  }
                }
                """;
            await File.WriteAllTextAsync(prodPath, prodContent, cancellationToken);
            files.Add(Path.GetRelativePath(context.WorkspacePath, prodPath));
        }

        return files;
    }

    private static async Task<List<string>> GeneratePythonConfigAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        var appDir = Path.Combine(context.WorkspacePath, "app");
        Directory.CreateDirectory(appDir);

        var configPath = Path.Combine(appDir, "config.py");
        if (!File.Exists(configPath))
        {
            var dbConfig = context.HasDatabase ? $$""""

                # Database
                db_host: str = "localhost"
                db_port: int = {{GetDefaultDbPort(context.DatabaseType)}}
                db_name: str = "{{context.ProjectName.Replace("-", "_")}}"
                db_user: str = ""
                db_password: str = ""

                @property
                def database_url(self) -> str:
                    return f"postgresql://{self.db_user}:{self.db_password}@{self.db_host}:{self.db_port}/{self.db_name}"
                """" : "";

            var content = $$$$""""
                """Application configuration using Pydantic settings."""
                from functools import lru_cache
                from pydantic_settings import BaseSettings


                class Settings(BaseSettings):
                    """Application settings loaded from environment variables."""

                    # Application
                    app_name: str = "{{{{context.ProjectName}}}}"
                    app_env: str = "development"
                    app_port: int = {{{{context.Port}}}}
                    debug: bool = False
                {{{{dbConfig}}}}
                    class Config:
                        env_file = ".env"
                        env_file_encoding = "utf-8"
                        case_sensitive = False


                @lru_cache
                def get_settings() -> Settings:
                    """Get cached settings instance."""
                    return Settings()
                """";

            await File.WriteAllTextAsync(configPath, content, cancellationToken);
            files.Add("app/config.py");
        }

        return files;
    }

    private static async Task<List<string>> GenerateNodeConfigAsync(ProjectContext context, CancellationToken cancellationToken)
    {
        var files = new List<string>();

        var srcDir = Path.Combine(context.WorkspacePath, "src");
        Directory.CreateDirectory(srcDir);

        var configPath = Path.Combine(srcDir, "config.ts");
        if (!File.Exists(configPath))
        {
            var dbConfig = context.HasDatabase ? $$"""

                  // Database
                  db: {
                    host: process.env.DB_HOST || 'localhost',
                    port: parseInt(process.env.DB_PORT || '{{GetDefaultDbPort(context.DatabaseType)}}', 10),
                    name: process.env.DB_NAME || '{{context.ProjectName.Replace("-", "_")}}',
                    user: process.env.DB_USER || '',
                    password: process.env.DB_PASSWORD || '',
                  },
                """ : "";

            var content = $$"""
                /**
                 * Application configuration loaded from environment variables.
                 */
                import dotenv from 'dotenv';

                // Load .env file in development
                if (process.env.NODE_ENV !== 'production') {
                  dotenv.config();
                }

                export const config = {
                  // Application
                  app: {
                    name: process.env.APP_NAME || '{{context.ProjectName}}',
                    env: process.env.APP_ENV || process.env.NODE_ENV || 'development',
                    port: parseInt(process.env.APP_PORT || '{{context.Port}}', 10),
                    debug: process.env.DEBUG === 'true',
                  },
                {{dbConfig}}
                } as const;

                export type Config = typeof config;
                """;

            await File.WriteAllTextAsync(configPath, content, cancellationToken);
            files.Add("src/config.ts");
        }

        return files;
    }
}
