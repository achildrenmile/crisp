using CRISP.Adr;
using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Models;

namespace CRISP.Agent;

/// <summary>
/// Records standard architectural decisions made during CRISP scaffolding.
/// These decisions are deterministic based on project requirements.
/// </summary>
public sealed class DecisionRecorder
{
    private readonly DecisionCollector _collector;

    public DecisionRecorder(DecisionCollector collector)
    {
        _collector = collector;
    }

    /// <summary>
    /// Records all decisions based on the execution plan.
    /// </summary>
    public void RecordDecisions(ExecutionPlan plan, CrispConfiguration config)
    {
        var req = plan.Requirements;
        var relatedFiles = plan.PlannedFiles.Select(f => f.RelativePath).ToList();

        // 1. Language decision
        RecordLanguageDecision(req, relatedFiles);

        // 2. Framework decision
        RecordFrameworkDecision(req, relatedFiles);

        // 3. Testing decision
        RecordTestingDecision(req, relatedFiles);

        // 4. CI/CD decision
        if (plan.Pipeline != null)
        {
            RecordCiCdDecision(req, plan.Pipeline, config);
        }

        // 5. Container decision
        if (req.IncludeContainerSupport)
        {
            RecordContainerDecision(req, relatedFiles);
        }

        // 6. SCM platform decision
        RecordScmDecision(req, config);

        // 7. Code style decision (if applicable)
        RecordCodeStyleDecision(req, relatedFiles);
    }

    private void RecordLanguageDecision(ProjectRequirements req, List<string> relatedFiles)
    {
        var (context, rationale, alternatives) = req.Language switch
        {
            ProjectLanguage.CSharp => (
                "The project requires a backend API. We need to choose a programming language that provides strong typing, good performance, and enterprise-level tooling.",
                "C# with .NET 10 provides excellent performance, strong typing with nullable reference types, mature ecosystem, and first-class async/await support. It's well-suited for enterprise applications with extensive IDE support.",
                new Dictionary<string, string>
                {
                    ["Java"] = "Similar capabilities but .NET has better async patterns and faster startup",
                    ["Go"] = "Faster startup but less mature ecosystem for web APIs",
                    ["Python"] = "Dynamic typing reduces compile-time safety"
                }),
            ProjectLanguage.Python => (
                "The project requires a backend API. We need a language that enables rapid development and has excellent library support.",
                "Python 3.12 offers rapid development, extensive library ecosystem, and excellent readability. Type hints provide optional static analysis while maintaining flexibility.",
                new Dictionary<string, string>
                {
                    ["JavaScript/Node.js"] = "Python has more mature data science and ML libraries",
                    ["Ruby"] = "Python has larger community and more web framework options",
                    ["Go"] = "Python prioritizes developer productivity over raw performance"
                }),
            ProjectLanguage.TypeScript => (
                "The project requires a JavaScript-based application. We need to decide between JavaScript and TypeScript.",
                "TypeScript provides static type checking, better IDE support, and catches errors at compile time. It compiles to JavaScript and integrates seamlessly with the Node.js ecosystem.",
                new Dictionary<string, string>
                {
                    ["JavaScript"] = "TypeScript adds type safety without losing JavaScript compatibility",
                    ["ReScript"] = "TypeScript has larger community and better ecosystem integration"
                }),
            ProjectLanguage.JavaScript => (
                "The project requires a JavaScript runtime application.",
                "JavaScript with Node.js provides a unified language across the stack, extensive npm ecosystem, and event-driven architecture suitable for I/O-intensive applications.",
                new Dictionary<string, string>
                {
                    ["TypeScript"] = "Plain JavaScript chosen for simplicity and faster iteration"
                }),
            ProjectLanguage.Java => (
                "The project requires a backend API with enterprise requirements.",
                "Java 21 provides mature ecosystem, excellent enterprise tooling, strong typing, and long-term support. Virtual threads improve concurrency handling significantly.",
                new Dictionary<string, string>
                {
                    ["Kotlin"] = "Java has broader enterprise adoption and tooling",
                    ["C#"] = "Java chosen for existing team expertise or JVM requirement"
                }),
            ProjectLanguage.Go => (
                "The project requires a high-performance backend service.",
                "Go provides excellent performance, simple concurrency with goroutines, fast compilation, and produces single static binaries ideal for containerized deployments.",
                new Dictionary<string, string>
                {
                    ["Rust"] = "Go has faster development cycle and simpler learning curve",
                    ["C#"] = "Go produces smaller container images and faster startup"
                }),
            ProjectLanguage.Rust => (
                "The project requires a high-performance backend with memory safety guarantees.",
                "Rust provides memory safety without garbage collection, excellent performance, and zero-cost abstractions. Ideal for performance-critical services.",
                new Dictionary<string, string>
                {
                    ["C++"] = "Rust provides memory safety guarantees at compile time",
                    ["Go"] = "Rust offers better performance for CPU-intensive workloads"
                }),
            ProjectLanguage.Dart => (
                "The project requires a Dart-based backend API.",
                "Dart 3.0 with null safety provides strong typing, excellent async support with isolates, and consistent development experience for teams also building Flutter applications.",
                new Dictionary<string, string>
                {
                    ["TypeScript"] = "Dart chosen for consistency with Flutter mobile development",
                    ["Go"] = "Dart provides familiar syntax for teams coming from Java/C#"
                }),
            _ => (
                "The project requires selecting a programming language.",
                $"{req.Language} was selected based on project requirements.",
                new Dictionary<string, string>())
        };

        _collector.Record(
            title: $"Use {req.Language} {req.RuntimeVersion} as runtime",
            context: context,
            decision: $"Use {req.Language} with {req.RuntimeVersion} as the programming language and runtime.",
            rationale: rationale,
            category: AdrCategory.Language,
            alternatives: alternatives,
            consequences:
            [
                $"Development uses {req.Language} language features and idioms",
                $"Team needs {req.Language} expertise",
                $"Dependencies managed via {GetPackageManager(req.Language)}"
            ],
            relatedFiles: relatedFiles.Where(f => IsSourceFile(f, req.Language)).Take(5).ToList());
    }

    private void RecordFrameworkDecision(ProjectRequirements req, List<string> relatedFiles)
    {
        var (context, rationale, alternatives) = req.Framework switch
        {
            ProjectFramework.AspNetCoreWebApi => (
                "We need a web framework for the C# REST API that provides routing, serialization, and middleware support.",
                "ASP.NET Core Web API is the standard choice for .NET APIs. It provides excellent performance, built-in dependency injection, OpenAPI support via Swashbuckle, and mature middleware pipeline.",
                new Dictionary<string, string>
                {
                    ["Minimal APIs"] = "Controller-based approach provides better organization for larger APIs",
                    ["Carter"] = "ASP.NET Core has better documentation and community support"
                }),
            ProjectFramework.FastApi => (
                "We need a Python web framework for building a REST API with automatic documentation.",
                "FastAPI provides automatic OpenAPI documentation, async support, Pydantic validation, and excellent performance. It's the modern standard for Python APIs.",
                new Dictionary<string, string>
                {
                    ["Flask"] = "FastAPI provides automatic OpenAPI docs and request validation",
                    ["Django REST Framework"] = "FastAPI is more lightweight and async-native"
                }),
            ProjectFramework.Flask => (
                "We need a lightweight Python web framework for building a REST API.",
                "Flask is minimalist and flexible, allowing custom architecture decisions. Good for smaller APIs or when maximum control is needed.",
                new Dictionary<string, string>
                {
                    ["FastAPI"] = "Flask chosen for its simplicity and flexibility",
                    ["Django"] = "Flask is more lightweight for simple APIs"
                }),
            ProjectFramework.Express => (
                "We need a Node.js web framework for building a REST API.",
                "Express.js is the de-facto standard for Node.js APIs. It's minimal, flexible, and has the largest middleware ecosystem.",
                new Dictionary<string, string>
                {
                    ["Fastify"] = "Express has larger ecosystem and more middleware options",
                    ["Koa"] = "Express has better documentation and community support"
                }),
            ProjectFramework.NestJs => (
                "We need a TypeScript web framework with enterprise architecture patterns.",
                "NestJS provides Angular-inspired architecture with decorators, dependency injection, and modular organization. Ideal for larger TypeScript applications.",
                new Dictionary<string, string>
                {
                    ["Express"] = "NestJS provides better structure for large applications",
                    ["Fastify"] = "NestJS has built-in architectural patterns"
                }),
            ProjectFramework.SpringBoot => (
                "We need a Java web framework for enterprise API development.",
                "Spring Boot is the industry standard for Java applications. It provides auto-configuration, dependency injection, and extensive enterprise integrations.",
                new Dictionary<string, string>
                {
                    ["Quarkus"] = "Spring Boot has larger ecosystem and more enterprise patterns",
                    ["Micronaut"] = "Spring Boot has better documentation and community"
                }),
            ProjectFramework.GinGonic => (
                "We need a Go web framework for building a high-performance REST API.",
                "Gin is the most popular Go web framework. It provides excellent performance, middleware support, and JSON validation.",
                new Dictionary<string, string>
                {
                    ["Echo"] = "Gin has slightly better performance benchmarks",
                    ["Chi"] = "Gin has larger community and more middleware"
                }),
            ProjectFramework.DartShelf => (
                "We need a Dart web framework for building a REST API.",
                "Shelf is the standard Dart HTTP middleware framework. Combined with shelf_router, it provides a flexible foundation for REST APIs.",
                new Dictionary<string, string>
                {
                    ["Dart Frog"] = "Shelf is more mature and flexible",
                    ["Conduit"] = "Shelf is more lightweight and composable"
                }),
            _ => (
                "We need a web framework for the API.",
                $"{req.Framework} was selected based on project requirements.",
                new Dictionary<string, string>())
        };

        _collector.Record(
            title: $"Use {FormatFrameworkName(req.Framework)} as web framework",
            context: context,
            decision: $"Use {FormatFrameworkName(req.Framework)} as the web framework for the REST API.",
            rationale: rationale,
            category: AdrCategory.Framework,
            alternatives: alternatives,
            consequences:
            [
                "API follows framework conventions and patterns",
                "Route definitions use framework-specific syntax",
                "Middleware pipeline follows framework architecture"
            ],
            relatedFiles: relatedFiles.Where(f => f.Contains("main") || f.Contains("app") || f.Contains("server")).Take(3).ToList());
    }

    private void RecordTestingDecision(ProjectRequirements req, List<string> relatedFiles)
    {
        var testFiles = relatedFiles.Where(f => f.Contains("test") || f.Contains("spec")).ToList();
        if (testFiles.Count == 0) return;

        var (testFramework, rationale, alternatives) = req.Language switch
        {
            ProjectLanguage.CSharp => ("xUnit",
                "xUnit is the modern testing framework for .NET with excellent async support and extensibility.",
                new Dictionary<string, string>
                {
                    ["NUnit"] = "xUnit has cleaner syntax and better parallel execution",
                    ["MSTest"] = "xUnit is more widely adopted in the community"
                }),
            ProjectLanguage.Python => ("pytest",
                "pytest is the de-facto standard for Python testing with powerful fixtures and plugins.",
                new Dictionary<string, string>
                {
                    ["unittest"] = "pytest has cleaner syntax and better fixture system"
                }),
            ProjectLanguage.TypeScript or ProjectLanguage.JavaScript => ("Jest",
                "Jest provides zero-config testing with built-in mocking, coverage, and snapshot testing.",
                new Dictionary<string, string>
                {
                    ["Mocha"] = "Jest requires less configuration and has built-in assertions",
                    ["Vitest"] = "Jest has larger ecosystem for existing projects"
                }),
            ProjectLanguage.Java => ("JUnit 5",
                "JUnit 5 is the standard Java testing framework with improved annotations and extension model.",
                new Dictionary<string, string>
                {
                    ["TestNG"] = "JUnit 5 has better IDE integration and community support"
                }),
            ProjectLanguage.Go => ("go test",
                "Go's built-in testing package provides table-driven tests and benchmarking without external dependencies.",
                new Dictionary<string, string>
                {
                    ["Testify"] = "Standard library is sufficient for most testing needs"
                }),
            ProjectLanguage.Dart => ("dart test",
                "Dart's built-in test package provides a comprehensive testing framework.",
                new Dictionary<string, string>
                {
                    ["Mockito"] = "Built-in mocking with mocktail is sufficient"
                }),
            _ => (req.TestingFramework ?? "default",
                "Standard testing framework for the language.",
                new Dictionary<string, string>())
        };

        _collector.Record(
            title: $"Use {testFramework} for testing",
            context: "We need a testing framework for unit and integration tests to ensure code quality and prevent regressions.",
            decision: $"Use {testFramework} as the testing framework.",
            rationale: rationale,
            category: AdrCategory.Testing,
            alternatives: alternatives,
            consequences:
            [
                "Tests follow framework conventions",
                "CI/CD pipeline runs tests on every commit",
                "Code coverage reports generated during builds"
            ],
            relatedFiles: testFiles.Take(3).ToList());
    }

    private void RecordCiCdDecision(ProjectRequirements req, PipelineDefinition pipeline, CrispConfiguration config)
    {
        var platform = req.ScmPlatform == ScmPlatform.GitHub ? "GitHub Actions" : "Azure Pipelines";

        _collector.Record(
            title: $"Use {platform} for CI/CD",
            context: "We need a CI/CD solution to automate building, testing, and potentially deploying the application.",
            decision: $"Use {platform} for continuous integration and deployment.",
            rationale: req.ScmPlatform == ScmPlatform.GitHub
                ? "GitHub Actions is tightly integrated with GitHub, provides generous free tier, and has extensive marketplace of reusable actions."
                : "Azure Pipelines integrates well with Azure DevOps Server, supports YAML and classic pipelines, and provides enterprise-grade features.",
            category: AdrCategory.CiCd,
            alternatives: req.ScmPlatform == ScmPlatform.GitHub
                ? new Dictionary<string, string>
                {
                    ["CircleCI"] = "GitHub Actions has better GitHub integration",
                    ["Jenkins"] = "GitHub Actions requires less maintenance"
                }
                : new Dictionary<string, string>
                {
                    ["Jenkins"] = "Azure Pipelines integrates better with Azure DevOps",
                    ["GitHub Actions"] = "Azure Pipelines works with on-premises Azure DevOps"
                },
            consequences:
            [
                $"Pipeline defined in {pipeline.FilePath}",
                $"Build steps: {string.Join(", ", pipeline.BuildSteps)}",
                "Automated testing on pull requests and merges"
            ],
            relatedFiles: [pipeline.FilePath]);
    }

    private void RecordContainerDecision(ProjectRequirements req, List<string> relatedFiles)
    {
        var dockerFiles = relatedFiles.Where(f =>
            f.Contains("Dockerfile", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("docker-compose", StringComparison.OrdinalIgnoreCase)).ToList();

        if (dockerFiles.Count == 0) return;

        _collector.Record(
            title: "Use Docker for containerization",
            context: "We need a containerization strategy for consistent development, testing, and deployment environments.",
            decision: "Use Docker with multi-stage builds for containerization.",
            rationale: "Docker provides consistent environments across development and production, enables easy scaling, and is the industry standard for container runtimes. Multi-stage builds reduce final image size.",
            category: AdrCategory.Deployment,
            alternatives: new Dictionary<string, string>
            {
                ["Podman"] = "Docker has better tooling and wider adoption",
                ["No containers"] = "Containers provide consistency across environments"
            },
            consequences:
            [
                "Application can be built and run with Docker",
                "Multi-stage builds keep production images small",
                "docker-compose available for local development"
            ],
            relatedFiles: dockerFiles);
    }

    private void RecordScmDecision(ProjectRequirements req, CrispConfiguration config)
    {
        _collector.Record(
            title: $"Use {req.ScmPlatform} for source control",
            context: "We need a source control platform to host the repository, manage code reviews, and integrate with CI/CD.",
            decision: $"Use {req.ScmPlatform} as the source control platform.",
            rationale: req.ScmPlatform == ScmPlatform.GitHub
                ? "GitHub provides excellent collaboration features, GitHub Actions for CI/CD, and is the most widely used platform for open source and private repositories."
                : "Azure DevOps Server provides on-premises hosting, enterprise security compliance, and integrated work item tracking.",
            category: AdrCategory.Infrastructure,
            alternatives: req.ScmPlatform == ScmPlatform.GitHub
                ? new Dictionary<string, string>
                {
                    ["GitLab"] = "GitHub has larger community and marketplace",
                    ["Azure DevOps"] = "GitHub preferred for cloud-hosted repos"
                }
                : new Dictionary<string, string>
                {
                    ["GitHub Enterprise"] = "Azure DevOps provides on-premises option",
                    ["GitLab"] = "Azure DevOps integrates with existing Microsoft tooling"
                },
            consequences:
            [
                $"Repository hosted on {req.ScmPlatform}",
                "Pull requests for code review",
                $"Branch protection on {config.Common.DefaultBranch}"
            ],
            relatedFiles: [".gitignore"]);
    }

    private void RecordCodeStyleDecision(ProjectRequirements req, List<string> relatedFiles)
    {
        var lintFiles = relatedFiles.Where(f =>
            f.Contains("eslint") ||
            f.Contains("ruff") ||
            f.Contains(".editorconfig") ||
            f.Contains("stylecop")).ToList();

        if (lintFiles.Count == 0) return;

        var (tool, rationale) = req.Language switch
        {
            ProjectLanguage.Python => ("Ruff", "Ruff is extremely fast, combines linting and formatting, and has Black compatibility."),
            ProjectLanguage.TypeScript or ProjectLanguage.JavaScript => ("ESLint", "ESLint is the standard linter with extensive plugin ecosystem."),
            ProjectLanguage.CSharp => ("EditorConfig + Roslyn analyzers", "Built-in analyzers enforce consistent code style."),
            _ => ("EditorConfig", "EditorConfig provides cross-editor consistency.")
        };

        _collector.Record(
            title: $"Use {tool} for code formatting",
            context: "We need consistent code formatting to improve readability and reduce merge conflicts.",
            decision: $"Use {tool} for code formatting and linting.",
            rationale: rationale,
            category: AdrCategory.CodeStyle,
            consequences:
            [
                "Consistent code style across the project",
                "Linting runs in CI/CD pipeline",
                "IDE integration for real-time feedback"
            ],
            relatedFiles: lintFiles);
    }

    private static string GetPackageManager(ProjectLanguage language) => language switch
    {
        ProjectLanguage.CSharp => "NuGet",
        ProjectLanguage.Python => "pip/PyPI",
        ProjectLanguage.TypeScript or ProjectLanguage.JavaScript => "npm",
        ProjectLanguage.Java => "Maven/Gradle",
        ProjectLanguage.Go => "Go modules",
        ProjectLanguage.Rust => "Cargo",
        ProjectLanguage.Dart => "pub.dev",
        _ => "package manager"
    };

    private static bool IsSourceFile(string path, ProjectLanguage language) => language switch
    {
        ProjectLanguage.CSharp => path.EndsWith(".cs"),
        ProjectLanguage.Python => path.EndsWith(".py"),
        ProjectLanguage.TypeScript => path.EndsWith(".ts") || path.EndsWith(".tsx"),
        ProjectLanguage.JavaScript => path.EndsWith(".js") || path.EndsWith(".jsx"),
        ProjectLanguage.Java => path.EndsWith(".java"),
        ProjectLanguage.Go => path.EndsWith(".go"),
        ProjectLanguage.Rust => path.EndsWith(".rs"),
        ProjectLanguage.Dart => path.EndsWith(".dart"),
        _ => false
    };

    private static string FormatFrameworkName(ProjectFramework framework) => framework switch
    {
        ProjectFramework.AspNetCoreWebApi => "ASP.NET Core Web API",
        ProjectFramework.FastApi => "FastAPI",
        ProjectFramework.Flask => "Flask",
        ProjectFramework.Django => "Django REST Framework",
        ProjectFramework.Express => "Express.js",
        ProjectFramework.NestJs => "NestJS",
        ProjectFramework.SpringBoot => "Spring Boot",
        ProjectFramework.Quarkus => "Quarkus",
        ProjectFramework.GinGonic => "Gin",
        ProjectFramework.Echo => "Echo",
        ProjectFramework.Actix => "Actix Web",
        ProjectFramework.React => "React",
        ProjectFramework.Vue => "Vue.js",
        ProjectFramework.NextJs => "Next.js",
        ProjectFramework.DartShelf => "Dart Shelf",
        ProjectFramework.DartFrog => "Dart Frog",
        _ => framework.ToString()
    };
}
