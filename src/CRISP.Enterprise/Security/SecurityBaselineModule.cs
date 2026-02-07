using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.Security;

/// <summary>
/// Generates security-related files and CI configuration to establish
/// a minimum security posture from day one.
/// </summary>
public sealed class SecurityBaselineModule : IEnterpriseModule
{
    private readonly ILogger<SecurityBaselineModule> _logger;

    public SecurityBaselineModule(ILogger<SecurityBaselineModule> logger)
    {
        _logger = logger;
    }

    public string Id => "security-baseline";
    public string DisplayName => "Security Baseline";
    public int Order => 100;

    public bool ShouldRun(ProjectContext context) => true; // Always runs

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        var filesCreated = new List<string>();

        try
        {
            // Generate SECURITY.md
            var securityMdPath = Path.Combine(context.WorkspacePath, "SECURITY.md");
            var securityContent = GenerateSecurityMd(context);
            await File.WriteAllTextAsync(securityMdPath, securityContent, cancellationToken);
            filesCreated.Add("SECURITY.md");

            // Append to .gitignore
            var gitignorePath = Path.Combine(context.WorkspacePath, ".gitignore");
            var secretPatterns = GenerateSecretPatterns();
            if (File.Exists(gitignorePath))
            {
                var existingContent = await File.ReadAllTextAsync(gitignorePath, cancellationToken);
                if (!existingContent.Contains("Secrets & credentials"))
                {
                    await File.AppendAllTextAsync(gitignorePath, "\n" + secretPatterns, cancellationToken);
                }
            }
            else
            {
                await File.WriteAllTextAsync(gitignorePath, secretPatterns, cancellationToken);
                filesCreated.Add(".gitignore");
            }

            // Generate .env.example
            var envExamplePath = Path.Combine(context.WorkspacePath, ".env.example");
            var envContent = GenerateEnvExample(context);
            await File.WriteAllTextAsync(envExamplePath, envContent, cancellationToken);
            filesCreated.Add(".env.example");

            // Record ADR
            context.DecisionCollector.Record(
                title: "Establish security baseline with vulnerability disclosure and secret scanning",
                context: "New projects need a minimum security posture from day one to prevent security debt.",
                decision: "Generate SECURITY.md with vulnerability disclosure policy, configure .gitignore to exclude secrets, and add .env.example for environment variables.",
                rationale: "Having security practices established from the start is easier than retrofitting them later. SECURITY.md provides a clear process for reporting vulnerabilities.",
                category: AdrCategory.Security,
                consequences: [
                    "Contributors know how to report security vulnerabilities",
                    "Sensitive files are excluded from version control by default",
                    "Environment variable structure is documented via .env.example"
                ],
                relatedFiles: filesCreated
            );

            _logger.LogInformation("Security baseline module completed: {FileCount} files created", filesCreated.Count);

            return new ModuleResult
            {
                ModuleId = Id,
                Success = true,
                FilesCreated = filesCreated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security baseline module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GenerateSecurityMd(ProjectContext context)
    {
        var securityEmail = context.SecurityContactEmail ?? context.TeamEmail ?? "[TEAM]@[ORGANIZATION].com";
        var orgName = context.OrganizationName ?? "the project maintainers";

        return $"""
            # Security Policy

            ## Reporting a Vulnerability

            If you discover a security vulnerability in this project, please report it responsibly.

            **Contact:** {securityEmail}
            **Response time:** We aim to acknowledge reports within 48 hours.

            ### Do

            - Email the security contact with a description of the vulnerability
            - Include steps to reproduce if possible
            - Allow reasonable time for a fix before public disclosure

            ### Don't

            - Open a public GitHub issue for security vulnerabilities
            - Exploit the vulnerability beyond what is necessary to demonstrate it

            ## Supported Versions

            | Version | Supported |
            |---------|-----------|
            | latest main | ✅ |

            ## Security Practices

            This project was scaffolded with the following security measures:

            - Dependency vulnerability scanning in CI
            - Secret scanning enabled
            - `.gitignore` configured to exclude sensitive files
            - No hardcoded credentials (environment-based configuration)

            ## Acknowledgments

            We appreciate the security research community and will acknowledge reporters (with their permission) after vulnerabilities are fixed.

            ---

            *This security policy was generated by [CRISP](https://github.com/strali/crisp) on behalf of {orgName}.*
            """;
    }

    private static string GenerateSecretPatterns()
    {
        return """

            # ── Secrets & credentials (added by CRISP Security Baseline) ──
            .env
            .env.*
            !.env.example
            *.pem
            *.key
            *.p12
            *.pfx
            *.jks
            **/secrets/
            **/credentials/
            appsettings.*.json
            !appsettings.json
            !appsettings.Development.json
            local.settings.json
            """;
    }

    private static string GenerateEnvExample(ProjectContext context)
    {
        var dbSection = context.HasDatabase ? $"""

            # ── Database ──
            # DB_HOST=localhost
            # DB_PORT={GetDefaultDbPort(context.DatabaseType)}
            # DB_NAME={context.ProjectName.Replace("-", "_")}
            # DB_USER=
            # DB_PASSWORD=
            """ : "";

        var secretsSection = context.SecretsManager != "none" ? $"""

            # ── Secrets Manager ({context.SecretsManager}) ──
            {GetSecretsManagerEnvVars(context.SecretsManager)}
            """ : "";

        return $"""
            # ── Application Configuration ──
            # Copy this file to .env and fill in real values.
            # NEVER commit .env to source control.

            APP_NAME={context.ProjectName}
            APP_ENV=development
            APP_PORT={context.Port}
            {dbSection}{secretsSection}
            """;
    }

    private static string GetDefaultDbPort(string? dbType) => dbType?.ToLowerInvariant() switch
    {
        "postgresql" or "postgres" => "5432",
        "mysql" or "mariadb" => "3306",
        "sqlserver" or "mssql" => "1433",
        "mongodb" => "27017",
        "redis" => "6379",
        _ => "5432"
    };

    private static string GetSecretsManagerEnvVars(string secretsManager) => secretsManager.ToLowerInvariant() switch
    {
        "azure-keyvault" => "# AZURE_KEYVAULT_URL=https://your-vault.vault.azure.net/",
        "aws-secrets" => "# AWS_SECRETS_REGION=us-east-1",
        "hashicorp-vault" => "# VAULT_ADDR=http://localhost:8200\n# VAULT_TOKEN=",
        _ => ""
    };
}
