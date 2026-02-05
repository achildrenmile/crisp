using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CRISP.Pipelines.Generators;

/// <summary>
/// Generator for Azure Pipelines YAML.
/// </summary>
public sealed class AzurePipelinesGenerator : IPipelineGenerator
{
    private readonly ILogger<AzurePipelinesGenerator> _logger;

    public AzurePipelinesGenerator(ILogger<AzurePipelinesGenerator> logger)
    {
        _logger = logger;
    }

    public ScmPlatform Platform => ScmPlatform.AzureDevOps;
    public PipelineFormat? Format => PipelineFormat.Yaml;

    public Task<PipelineGenerationResult> GeneratePipelineAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Azure Pipelines YAML for {ProjectName}", requirements.ProjectName);

        var pipeline = requirements.Language switch
        {
            ProjectLanguage.CSharp => GenerateDotNetPipeline(requirements),
            ProjectLanguage.Python => GeneratePythonPipeline(requirements),
            ProjectLanguage.TypeScript or ProjectLanguage.JavaScript => GenerateNodePipeline(requirements),
            _ => GenerateGenericPipeline(requirements)
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var content = serializer.Serialize(pipeline);
        var buildSteps = GetBuildSteps(requirements);

        return Task.FromResult(new PipelineGenerationResult
        {
            Success = true,
            Content = content,
            FileName = "azure-pipelines.yml",
            FilePath = "azure-pipelines.yml",
            Description = $"Azure Pipelines CI/CD for {requirements.Language}",
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
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
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

    private static Dictionary<string, object> GenerateDotNetPipeline(ProjectRequirements requirements)
    {
        return new Dictionary<string, object>
        {
            ["trigger"] = new List<string> { "main" },
            ["pr"] = new List<string> { "main" },
            ["pool"] = new Dictionary<string, object>
            {
                ["vmImage"] = "ubuntu-latest"
            },
            ["variables"] = new Dictionary<string, object>
            {
                ["buildConfiguration"] = "Release"
            },
            ["stages"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["stage"] = "Build",
                    ["displayName"] = "Build and Test",
                    ["jobs"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["job"] = "Build",
                            ["displayName"] = "Build",
                            ["steps"] = GetDotNetSteps(requirements)
                        }
                    }
                }
            }
        };
    }

    private static List<object> GetDotNetSteps(ProjectRequirements requirements)
    {
        var steps = new List<object>
        {
            new Dictionary<string, object>
            {
                ["task"] = "UseDotNet@2",
                ["displayName"] = "Use .NET 8",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["packageType"] = "sdk",
                    ["version"] = "8.0.x"
                }
            },
            new Dictionary<string, object>
            {
                ["task"] = "DotNetCoreCLI@2",
                ["displayName"] = "Restore",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["command"] = "restore",
                    ["projects"] = "**/*.csproj"
                }
            },
            new Dictionary<string, object>
            {
                ["task"] = "DotNetCoreCLI@2",
                ["displayName"] = "Build",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["command"] = "build",
                    ["projects"] = "**/*.csproj",
                    ["arguments"] = "--configuration $(buildConfiguration) --no-restore"
                }
            },
            new Dictionary<string, object>
            {
                ["task"] = "DotNetCoreCLI@2",
                ["displayName"] = "Test",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["command"] = "test",
                    ["projects"] = "**/*Tests.csproj",
                    ["arguments"] = "--configuration $(buildConfiguration) --no-build --collect:\"XPlat Code Coverage\""
                }
            },
            new Dictionary<string, object>
            {
                ["task"] = "PublishCodeCoverageResults@2",
                ["displayName"] = "Publish Code Coverage",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["summaryFileLocation"] = "$(Agent.TempDirectory)/**/coverage.cobertura.xml"
                }
            }
        };

        if (requirements.IncludeContainerSupport)
        {
            steps.Add(new Dictionary<string, object>
            {
                ["task"] = "Docker@2",
                ["displayName"] = "Build Docker image",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["command"] = "build",
                    ["dockerfile"] = "Dockerfile",
                    ["tags"] = "$(Build.BuildId)"
                }
            });
        }

        steps.Add(new Dictionary<string, object>
        {
            ["task"] = "DotNetCoreCLI@2",
            ["displayName"] = "Publish",
            ["inputs"] = new Dictionary<string, object>
            {
                ["command"] = "publish",
                ["publishWebProjects"] = true,
                ["arguments"] = "--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)",
                ["zipAfterPublish"] = true
            }
        });

        steps.Add(new Dictionary<string, object>
        {
            ["task"] = "PublishBuildArtifacts@1",
            ["displayName"] = "Publish Artifacts",
            ["inputs"] = new Dictionary<string, object>
            {
                ["pathToPublish"] = "$(Build.ArtifactStagingDirectory)",
                ["artifactName"] = "drop"
            }
        });

        return steps;
    }

    private static Dictionary<string, object> GeneratePythonPipeline(ProjectRequirements requirements)
    {
        var steps = new List<object>
        {
            new Dictionary<string, object>
            {
                ["task"] = "UsePythonVersion@0",
                ["displayName"] = "Use Python",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["versionSpec"] = requirements.RuntimeVersion.Replace("Python ", "")
                }
            },
            new Dictionary<string, object>
            {
                ["script"] = "pip install -r requirements.txt",
                ["displayName"] = "Install dependencies"
            }
        };

        if (requirements.LintingTools.Contains("Ruff"))
        {
            steps.Add(new Dictionary<string, object>
            {
                ["script"] = "pip install ruff && ruff check .",
                ["displayName"] = "Lint with Ruff"
            });
        }

        steps.Add(new Dictionary<string, object>
        {
            ["script"] = "pip install pytest pytest-cov pytest-azurepipelines && pytest --cov=app --cov-report=xml tests/",
            ["displayName"] = "Run tests"
        });

        steps.Add(new Dictionary<string, object>
        {
            ["task"] = "PublishCodeCoverageResults@2",
            ["displayName"] = "Publish Code Coverage",
            ["inputs"] = new Dictionary<string, object>
            {
                ["summaryFileLocation"] = "$(System.DefaultWorkingDirectory)/coverage.xml"
            }
        });

        if (requirements.IncludeContainerSupport)
        {
            steps.Add(new Dictionary<string, object>
            {
                ["task"] = "Docker@2",
                ["displayName"] = "Build Docker image",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["command"] = "build",
                    ["dockerfile"] = "Dockerfile",
                    ["tags"] = "$(Build.BuildId)"
                }
            });
        }

        return new Dictionary<string, object>
        {
            ["trigger"] = new List<string> { "main" },
            ["pr"] = new List<string> { "main" },
            ["pool"] = new Dictionary<string, object>
            {
                ["vmImage"] = "ubuntu-latest"
            },
            ["stages"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["stage"] = "Build",
                    ["displayName"] = "Build and Test",
                    ["jobs"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["job"] = "Build",
                            ["displayName"] = "Build",
                            ["steps"] = steps
                        }
                    }
                }
            }
        };
    }

    private static Dictionary<string, object> GenerateNodePipeline(ProjectRequirements requirements)
    {
        var steps = new List<object>
        {
            new Dictionary<string, object>
            {
                ["task"] = "NodeTool@0",
                ["displayName"] = "Use Node.js",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["versionSpec"] = requirements.RuntimeVersion.Replace("Node ", "")
                }
            },
            new Dictionary<string, object>
            {
                ["script"] = "npm ci",
                ["displayName"] = "Install dependencies"
            }
        };

        if (requirements.LintingTools.Any(t => t.Contains("ESLint")))
        {
            steps.Add(new Dictionary<string, object>
            {
                ["script"] = "npm run lint",
                ["displayName"] = "Lint"
            });
        }

        steps.Add(new Dictionary<string, object>
        {
            ["script"] = "npm run build",
            ["displayName"] = "Build"
        });

        steps.Add(new Dictionary<string, object>
        {
            ["script"] = "npm test -- --coverage",
            ["displayName"] = "Test"
        });

        if (requirements.IncludeContainerSupport)
        {
            steps.Add(new Dictionary<string, object>
            {
                ["task"] = "Docker@2",
                ["displayName"] = "Build Docker image",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["command"] = "build",
                    ["dockerfile"] = "Dockerfile",
                    ["tags"] = "$(Build.BuildId)"
                }
            });
        }

        return new Dictionary<string, object>
        {
            ["trigger"] = new List<string> { "main" },
            ["pr"] = new List<string> { "main" },
            ["pool"] = new Dictionary<string, object>
            {
                ["vmImage"] = "ubuntu-latest"
            },
            ["stages"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["stage"] = "Build",
                    ["displayName"] = "Build and Test",
                    ["jobs"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["job"] = "Build",
                            ["displayName"] = "Build",
                            ["steps"] = steps
                        }
                    }
                }
            }
        };
    }

    private static Dictionary<string, object> GenerateGenericPipeline(ProjectRequirements requirements)
    {
        return new Dictionary<string, object>
        {
            ["trigger"] = new List<string> { "main" },
            ["pr"] = new List<string> { "main" },
            ["pool"] = new Dictionary<string, object>
            {
                ["vmImage"] = "ubuntu-latest"
            },
            ["stages"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["stage"] = "Build",
                    ["displayName"] = "Build and Test",
                    ["jobs"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["job"] = "Build",
                            ["displayName"] = "Build",
                            ["steps"] = new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    ["script"] = "echo \"Add your build commands here\"",
                                    ["displayName"] = "Build"
                                },
                                new Dictionary<string, object>
                                {
                                    ["script"] = "echo \"Add your test commands here\"",
                                    ["displayName"] = "Test"
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private static IReadOnlyList<string> GetBuildSteps(ProjectRequirements requirements)
    {
        var steps = new List<string>();

        switch (requirements.Language)
        {
            case ProjectLanguage.CSharp:
                steps.AddRange(["Setup .NET 8", "Restore", "Build", "Test", "Publish coverage", "Publish artifacts"]);
                break;
            case ProjectLanguage.Python:
                steps.AddRange(["Setup Python", "Install dependencies"]);
                if (requirements.LintingTools.Contains("Ruff"))
                {
                    steps.Add("Lint with Ruff");
                }
                steps.AddRange(["Run pytest", "Publish coverage"]);
                break;
            case ProjectLanguage.TypeScript:
            case ProjectLanguage.JavaScript:
                steps.AddRange(["Setup Node.js", "Install dependencies"]);
                if (requirements.LintingTools.Any(t => t.Contains("ESLint")))
                {
                    steps.Add("Lint");
                }
                steps.AddRange(["Build", "Test"]);
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
