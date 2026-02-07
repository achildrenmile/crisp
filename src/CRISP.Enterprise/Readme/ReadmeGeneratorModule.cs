using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.Readme;

/// <summary>
/// Generates a comprehensive, useful README.md using all available project context.
/// </summary>
public sealed class ReadmeGeneratorModule : IEnterpriseModule
{
    private readonly ILogger<ReadmeGeneratorModule> _logger;

    public ReadmeGeneratorModule(ILogger<ReadmeGeneratorModule> logger)
    {
        _logger = logger;
    }

    public string Id => "readme";
    public string DisplayName => "README Generator";
    public int Order => 700;

    public bool ShouldRun(ProjectContext context) => true;

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var readmePath = Path.Combine(context.WorkspacePath, "README.md");
            var readmeContent = GenerateReadme(context);
            await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);

            // Record ADR
            context.DecisionCollector.Record(
                title: "Generate comprehensive README with quick-start and documentation cross-references",
                context: "Every project needs clear documentation for onboarding developers and explaining the project's purpose.",
                decision: "Generate a comprehensive README.md with project overview, quick-start guide, architecture summary, and links to other documentation.",
                rationale: "A good README reduces onboarding time and serves as the project's landing page. Cross-referencing other docs (CONTRIBUTING, SECURITY, ADRs) improves discoverability.",
                category: AdrCategory.Documentation,
                consequences: [
                    "Developers can get started quickly with clear instructions",
                    "Project structure and architecture are documented",
                    "Related documentation is easy to find"
                ],
                relatedFiles: ["README.md"]
            );

            return new ModuleResult
            {
                ModuleId = Id,
                Success = true,
                FilesCreated = ["README.md"]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "README generator module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GenerateReadme(ProjectContext context)
    {
        var description = context.ProjectDescription ?? $"A {context.Framework} application.";
        var prerequisites = GetPrerequisites(context);
        var installCommand = GetInstallCommand(context);
        var runCommand = GetRunCommand(context);
        var testCommand = GetTestCommand(context);
        var projectTree = GenerateProjectTree(context);
        var architecture = GenerateArchitectureSection(context);
        var pipelineDescription = GetPipelineDescription(context);
        var licenseSection = GetLicenseSection(context);

        var contributingLink = context.GeneratedFiles.Contains("CONTRIBUTING.md")
            ? "See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines."
            : "";

        var securityLink = context.GeneratedFiles.Contains("SECURITY.md")
            ? "See [SECURITY.md](SECURITY.md) for vulnerability reporting."
            : "";

        var adrLink = context.GeneratedFiles.Any(f => f.Contains("docs/adr"))
            ? "\n\nFor detailed architecture decisions, see [Architecture Decision Records](docs/adr/README.md)."
            : "";

        return $"""
            # {context.ProjectName}

            {description}

            ## Quick Start

            ### Prerequisites

            {prerequisites}

            ### Setup

            ```bash
            git clone {context.RepositoryUrl}
            cd {context.ProjectName}
            {installCommand}
            ```

            ### Run

            ```bash
            {runCommand}
            ```

            The application will be available at `http://localhost:{context.Port}`.

            ### Test

            ```bash
            {testCommand}
            ```

            ## Project Structure

            ```
            {projectTree}
            ```

            ## Architecture

            {architecture}{adrLink}

            ## CI/CD

            {pipelineDescription}

            ## API Endpoints

            {GetApiEndpointsSection(context)}

            ## Contributing

            {contributingLink}

            ## Security

            {securityLink}

            ## License

            {licenseSection}

            ---

            *This project was scaffolded by [CRISP](https://github.com/strali/crisp).*
            """;
    }

    private static string GetPrerequisites(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => $"- [.NET {context.Runtime.Replace(".NET ", "")} SDK](https://dotnet.microsoft.com/download)",
        "python" => $"- [Python {context.Runtime.Replace("Python ", "")}](https://www.python.org/downloads/)\n- pip",
        "typescript" or "javascript" => $"- [Node.js {context.Runtime.Replace("Node ", "")}+](https://nodejs.org/)\n- npm or yarn",
        "java" => $"- [JDK {context.Runtime.Replace("Java ", "")}](https://adoptium.net/)\n- Maven or Gradle",
        "dart" => $"- [Dart SDK {context.Runtime.Replace("Dart ", "")}](https://dart.dev/get-dart)",
        _ => "- See project documentation for requirements"
    };

    private static string GetInstallCommand(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => "dotnet restore",
        "python" => "pip install -r requirements.txt",
        "typescript" or "javascript" => "npm install",
        "java" => "mvn install -DskipTests",
        "dart" => "dart pub get",
        _ => "# Install dependencies"
    };

    private static string GetRunCommand(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => "dotnet run",
        "python" => context.Framework.ToLowerInvariant().Contains("fastapi")
            ? "uvicorn app.main:app --reload"
            : "python main.py",
        "typescript" or "javascript" => "npm run dev",
        "java" => "mvn spring-boot:run",
        "dart" => "dart run bin/server.dart",
        _ => "# Run the application"
    };

    private static string GetTestCommand(ProjectContext context) => context.Language.ToLowerInvariant() switch
    {
        "csharp" => "dotnet test",
        "python" => "pytest",
        "typescript" or "javascript" => "npm test",
        "java" => "mvn test",
        "dart" => "dart test",
        _ => "# Run tests"
    };

    private static string GenerateProjectTree(ProjectContext context)
    {
        // Generate a simplified tree based on language
        return context.Language.ToLowerInvariant() switch
        {
            "csharp" => $"""
                {context.ProjectName}/
                ├── src/
                │   └── {context.ProjectName}/
                │       ├── Controllers/
                │       ├── Models/
                │       ├── Services/
                │       └── Program.cs
                ├── tests/
                ├── docs/
                ├── Dockerfile
                └── README.md
                """,
            "python" => $"""
                {context.ProjectName}/
                ├── app/
                │   ├── __init__.py
                │   ├── main.py
                │   ├── routes/
                │   └── models/
                ├── tests/
                ├── docs/
                ├── requirements.txt
                ├── Dockerfile
                └── README.md
                """,
            "typescript" or "javascript" => $"""
                {context.ProjectName}/
                ├── src/
                │   ├── index.ts
                │   ├── routes/
                │   └── services/
                ├── tests/
                ├── docs/
                ├── package.json
                ├── Dockerfile
                └── README.md
                """,
            "dart" => $"""
                {context.ProjectName}/
                ├── bin/
                │   └── server.dart
                ├── lib/
                │   └── src/
                ├── test/
                ├── docs/
                ├── pubspec.yaml
                ├── Dockerfile
                └── README.md
                """,
            _ => $"""
                {context.ProjectName}/
                ├── src/
                ├── tests/
                ├── docs/
                └── README.md
                """
        };
    }

    private static string GenerateArchitectureSection(ProjectContext context)
    {
        var framework = context.Framework;
        var dbInfo = context.HasDatabase ? $" with {context.DatabaseType} for data persistence" : "";
        var dockerInfo = context.HasDocker ? " Containerized with Docker for consistent deployments." : "";

        return $"This project uses **{framework}** as the web framework{dbInfo}.{dockerInfo}";
    }

    private static string GetPipelineDescription(ProjectContext context)
    {
        if (string.IsNullOrEmpty(context.CiPipelineFile))
        {
            return "CI/CD pipeline not configured.";
        }

        var platform = context.ScmPlatform.Equals("github", StringComparison.OrdinalIgnoreCase)
            ? "GitHub Actions"
            : "Azure Pipelines";

        var stages = "lint → test → build";
        if (context.GeneratedFiles.Any(f => f.Contains("sbom")))
        {
            stages += " → SBOM";
        }
        if (context.HasDocker)
        {
            stages += " → Docker build";
        }

        return $"""
            **{platform}** workflow at `{context.CiPipelineFile}`.

            Pipeline stages: {stages}

            The pipeline runs on every push to `{context.DefaultBranch}` and on pull requests.
            """;
    }

    private static string GetApiEndpointsSection(ProjectContext context)
    {
        if (!context.IsApiProject)
        {
            return "This project does not expose HTTP endpoints.";
        }

        return $"""
            | Endpoint | Method | Description |
            |----------|--------|-------------|
            | `/healthz` | GET | Liveness probe |
            | `/ready` | GET | Readiness probe |
            | `/api/...` | * | Application endpoints |

            API documentation is available at `http://localhost:{context.Port}/swagger` (if configured).
            """;
    }

    private static string GetLicenseSection(ProjectContext context)
    {
        var licenseName = context.LicenseSpdx.ToUpperInvariant() switch
        {
            "MIT" => "MIT License",
            "APACHE-2.0" => "Apache 2.0 License",
            "BSD-2-CLAUSE" => "BSD 2-Clause License",
            "BSD-3-CLAUSE" => "BSD 3-Clause License",
            "GPL-3.0-ONLY" => "GPL 3.0 License",
            "ISC" => "ISC License",
            _ => "Proprietary"
        };

        return context.GeneratedFiles.Contains("LICENSE")
            ? $"This project is licensed under the {licenseName} — see [LICENSE](LICENSE) for details."
            : $"This project is licensed under the {licenseName}.";
    }
}
