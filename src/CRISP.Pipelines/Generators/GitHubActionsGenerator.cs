using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CRISP.Pipelines.Generators;

/// <summary>
/// Generator for GitHub Actions workflows.
/// </summary>
public sealed class GitHubActionsGenerator : IPipelineGenerator
{
    private readonly ILogger<GitHubActionsGenerator> _logger;

    public GitHubActionsGenerator(ILogger<GitHubActionsGenerator> logger)
    {
        _logger = logger;
    }

    public ScmPlatform Platform => ScmPlatform.GitHub;
    public PipelineFormat? Format => null;

    public Task<PipelineGenerationResult> GeneratePipelineAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating GitHub Actions workflow for {ProjectName}", requirements.ProjectName);

        var workflow = requirements.Language switch
        {
            ProjectLanguage.CSharp => GenerateDotNetWorkflow(requirements),
            ProjectLanguage.Python => GeneratePythonWorkflow(requirements),
            ProjectLanguage.TypeScript or ProjectLanguage.JavaScript => GenerateNodeWorkflow(requirements),
            _ => GenerateGenericWorkflow(requirements)
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var content = serializer.Serialize(workflow);
        var buildSteps = GetBuildSteps(requirements);

        return Task.FromResult(new PipelineGenerationResult
        {
            Success = true,
            Content = content,
            FileName = "ci.yml",
            FilePath = ".github/workflows/ci.yml",
            Description = $"GitHub Actions CI workflow for {requirements.Language}",
            BuildSteps = buildSteps
        });
    }

    public Task<PipelineValidationResult> ValidatePipelineAsync(
        string pipelineContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .Build();

            deserializer.Deserialize<Dictionary<string, object>>(pipelineContent);

            return Task.FromResult(new PipelineValidationResult
            {
                IsValid = true
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PipelineValidationResult
            {
                IsValid = false,
                Errors = [$"YAML parsing error: {ex.Message}"]
            });
        }
    }

    private static Dictionary<string, object> GenerateDotNetWorkflow(ProjectRequirements requirements)
    {
        var steps = new List<Dictionary<string, object>>
        {
            new()
            {
                ["uses"] = "actions/checkout@v4"
            },
            new()
            {
                ["name"] = "Setup .NET",
                ["uses"] = "actions/setup-dotnet@v4",
                ["with"] = new Dictionary<string, object>
                {
                    ["dotnet-version"] = "10.0.x"
                }
            },
            new()
            {
                ["name"] = "Restore dependencies",
                ["run"] = "dotnet restore"
            },
            new()
            {
                ["name"] = "Build",
                ["run"] = "dotnet build --no-restore --configuration Release"
            },
            new()
            {
                ["name"] = "Test",
                ["run"] = "dotnet test --no-build --configuration Release --verbosity normal --collect:\"XPlat Code Coverage\""
            }
        };

        if (requirements.IncludeContainerSupport)
        {
            steps.Add(new Dictionary<string, object>
            {
                ["name"] = "Build Docker image",
                ["run"] = $"docker build -t {requirements.ProjectName}:${{{{ github.sha }}}} ."
            });
        }

        return CreateWorkflow("CI", steps);
    }

    private static Dictionary<string, object> GeneratePythonWorkflow(ProjectRequirements requirements)
    {
        var steps = new List<Dictionary<string, object>>
        {
            new()
            {
                ["uses"] = "actions/checkout@v4"
            },
            new()
            {
                ["name"] = "Set up Python",
                ["uses"] = "actions/setup-python@v5",
                ["with"] = new Dictionary<string, object>
                {
                    ["python-version"] = requirements.RuntimeVersion.Replace("Python ", "")
                }
            },
            new()
            {
                ["name"] = "Install dependencies",
                ["run"] = "pip install -r requirements.txt"
            }
        };

        if (requirements.LintingTools.Contains("Ruff"))
        {
            steps.Add(new Dictionary<string, object>
            {
                ["name"] = "Lint with Ruff",
                ["run"] = "pip install ruff && ruff check ."
            });
        }

        steps.Add(new Dictionary<string, object>
        {
            ["name"] = "Test with pytest",
            ["run"] = "pip install pytest pytest-cov && pytest --cov=app tests/"
        });

        if (requirements.IncludeContainerSupport)
        {
            steps.Add(new Dictionary<string, object>
            {
                ["name"] = "Build Docker image",
                ["run"] = $"docker build -t {requirements.ProjectName}:${{{{ github.sha }}}} ."
            });
        }

        return CreateWorkflow("CI", steps);
    }

    private static Dictionary<string, object> GenerateNodeWorkflow(ProjectRequirements requirements)
    {
        var steps = new List<Dictionary<string, object>>
        {
            new()
            {
                ["uses"] = "actions/checkout@v4"
            },
            new()
            {
                ["name"] = "Setup Node.js",
                ["uses"] = "actions/setup-node@v4",
                ["with"] = new Dictionary<string, object>
                {
                    ["node-version"] = requirements.RuntimeVersion.Replace("Node ", ""),
                    ["cache"] = "npm"
                }
            },
            new()
            {
                ["name"] = "Install dependencies",
                ["run"] = "npm ci"
            }
        };

        if (requirements.LintingTools.Any(t => t.Contains("ESLint")))
        {
            steps.Add(new Dictionary<string, object>
            {
                ["name"] = "Lint",
                ["run"] = "npm run lint"
            });
        }

        steps.Add(new Dictionary<string, object>
        {
            ["name"] = "Build",
            ["run"] = "npm run build"
        });

        steps.Add(new Dictionary<string, object>
        {
            ["name"] = "Test",
            ["run"] = "npm test"
        });

        if (requirements.IncludeContainerSupport)
        {
            steps.Add(new Dictionary<string, object>
            {
                ["name"] = "Build Docker image",
                ["run"] = $"docker build -t {requirements.ProjectName}:${{{{ github.sha }}}} ."
            });
        }

        return CreateWorkflow("CI", steps);
    }

    private static Dictionary<string, object> GenerateGenericWorkflow(ProjectRequirements requirements)
    {
        var steps = new List<Dictionary<string, object>>
        {
            new()
            {
                ["uses"] = "actions/checkout@v4"
            },
            new()
            {
                ["name"] = "Build",
                ["run"] = "echo \"Add your build commands here\""
            },
            new()
            {
                ["name"] = "Test",
                ["run"] = "echo \"Add your test commands here\""
            }
        };

        return CreateWorkflow("CI", steps);
    }

    private static Dictionary<string, object> CreateWorkflow(
        string name,
        List<Dictionary<string, object>> steps)
    {
        return new Dictionary<string, object>
        {
            ["name"] = name,
            ["on"] = new Dictionary<string, object>
            {
                ["push"] = new Dictionary<string, object>
                {
                    ["branches"] = new List<string> { "main" }
                },
                ["pull_request"] = new Dictionary<string, object>
                {
                    ["branches"] = new List<string> { "main" }
                }
            },
            ["jobs"] = new Dictionary<string, object>
            {
                ["build"] = new Dictionary<string, object>
                {
                    ["runs-on"] = "ubuntu-latest",
                    ["steps"] = steps
                }
            }
        };
    }

    private static IReadOnlyList<string> GetBuildSteps(ProjectRequirements requirements)
    {
        var steps = new List<string> { "Checkout code" };

        switch (requirements.Language)
        {
            case ProjectLanguage.CSharp:
                steps.AddRange(["Setup .NET 10", "Restore dependencies", "Build", "Run tests"]);
                break;
            case ProjectLanguage.Python:
                steps.AddRange(["Setup Python", "Install dependencies"]);
                if (requirements.LintingTools.Contains("Ruff"))
                {
                    steps.Add("Lint with Ruff");
                }
                steps.Add("Run pytest");
                break;
            case ProjectLanguage.TypeScript:
            case ProjectLanguage.JavaScript:
                steps.AddRange(["Setup Node.js", "Install dependencies"]);
                if (requirements.LintingTools.Any(t => t.Contains("ESLint")))
                {
                    steps.Add("Lint");
                }
                steps.AddRange(["Build", "Run tests"]);
                break;
            default:
                steps.AddRange(["Build", "Test"]);
                break;
        }

        if (requirements.IncludeContainerSupport)
        {
            steps.Add("Build Docker image");
        }

        return steps;
    }
}
