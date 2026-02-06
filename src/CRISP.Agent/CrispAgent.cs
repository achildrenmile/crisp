using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRISP.Agent;

/// <summary>
/// Main CRISP agent orchestrator implementation.
/// </summary>
public sealed class CrispAgent : ICrispAgent
{
    private readonly ILogger<CrispAgent> _logger;
    private readonly CrispConfiguration _config;
    private readonly ITemplateEngine _templateEngine;
    private readonly IEnumerable<IPipelineGenerator> _pipelineGenerators;
    private readonly ISourceControlProvider _sourceControlProvider;
    private readonly IGitOperations _gitOperations;
    private readonly IFilesystemOperations _filesystemOperations;
    private readonly IAuditLogger _auditLogger;
    private readonly IPolicyEngine _policyEngine;

    private const int MaxRemediationAttempts = 3;

    public CrispAgent(
        ILogger<CrispAgent> logger,
        IOptions<CrispConfiguration> config,
        ITemplateEngine templateEngine,
        IEnumerable<IPipelineGenerator> pipelineGenerators,
        ISourceControlProvider sourceControlProvider,
        IGitOperations gitOperations,
        IFilesystemOperations filesystemOperations,
        IAuditLogger auditLogger,
        IPolicyEngine policyEngine)
    {
        _logger = logger;
        _config = config.Value;
        _templateEngine = templateEngine;
        _pipelineGenerators = pipelineGenerators;
        _sourceControlProvider = sourceControlProvider;
        _gitOperations = gitOperations;
        _filesystemOperations = filesystemOperations;
        _auditLogger = auditLogger;
        _policyEngine = policyEngine;
    }

    public Guid SessionId => _auditLogger.SessionId;

    public event EventHandler<ExecutionPlanEventArgs>? PlanReadyForApproval;
    public event EventHandler<ExecutionStepEventArgs>? StepCompleted;

    public async Task<ExecutionPlan> CreatePlanAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating execution plan for project: {ProjectName}", requirements.ProjectName);

        await _auditLogger.LogActionAsync(
            "agent.create_plan",
            ExecutionPhase.Planning,
            ActionResult.Pending,
            $"Creating plan for {requirements.ProjectName}");

        // Get available templates
        var templates = await _templateEngine.GetAvailableTemplatesAsync(requirements, cancellationToken);
        var selectedTemplate = templates.FirstOrDefault()
            ?? throw new InvalidOperationException($"No template found for {requirements.Framework}");

        // Get planned files
        var plannedFiles = await _templateEngine.GetPlannedFilesAsync(selectedTemplate, requirements, cancellationToken);

        // Get pipeline generator
        var pipelineGenerator = _pipelineGenerators.FirstOrDefault(g =>
            g.Platform == requirements.ScmPlatform &&
            (g.Format == null || g.Format == _config.AzureDevOps.PipelineFormat));

        PipelineDefinition? pipelineDefinition = null;
        if (_config.Common.GenerateCiCd && pipelineGenerator != null)
        {
            var pipelineResult = await pipelineGenerator.GeneratePipelineAsync(requirements, cancellationToken);
            if (pipelineResult.Success)
            {
                pipelineDefinition = new PipelineDefinition
                {
                    FileName = pipelineResult.FileName,
                    FilePath = pipelineResult.FilePath,
                    TriggerDescription = "On push to main and pull requests",
                    BuildSteps = pipelineResult.BuildSteps
                };
            }
        }

        // Create repository details
        var repositoryDetails = new RepositoryDetails
        {
            Name = requirements.ProjectName,
            Owner = requirements.ScmPlatform == ScmPlatform.GitHub
                ? _config.GitHub.Owner
                : _config.AzureDevOps.Project ?? "DefaultProject",
            Visibility = requirements.Visibility.ToString().ToLowerInvariant(),
            DefaultBranch = _config.Common.DefaultBranch
        };

        // Validate against policies
        var tempPlan = new ExecutionPlan
        {
            Requirements = requirements,
            Template = selectedTemplate,
            PlannedFiles = plannedFiles.ToList(),
            Repository = repositoryDetails,
            Pipeline = pipelineDefinition,
            PolicyResults = [],
            Summary = "",
            Steps = []
        };

        var policyResults = await _policyEngine.ValidatePlanAsync(requirements, tempPlan, cancellationToken);

        // Create execution steps
        var steps = new List<ExecutionStep>
        {
            new() { StepNumber = 1, Description = $"Select template: {selectedTemplate.Name}", Operation = "template.select" },
            new() { StepNumber = 2, Description = $"Create workspace and scaffold {plannedFiles.Count} files", Operation = "filesystem.scaffold" },
            new() { StepNumber = 3, Description = $"Create {requirements.ScmPlatform} repository: {repositoryDetails.Owner}/{requirements.ProjectName}", Operation = "scm.create_repository" },
            new() { StepNumber = 4, Description = "Initialize Git and create initial commit", Operation = "git.init_commit" },
            new() { StepNumber = 5, Description = $"Push to {_config.Common.DefaultBranch} branch", Operation = "git.push" }
        };

        if (_config.Common.GenerateCiCd && pipelineDefinition != null)
        {
            steps.Add(new ExecutionStep
            {
                StepNumber = 6,
                Description = "Trigger initial CI/CD pipeline run",
                Operation = "scm.trigger_pipeline"
            });
            steps.Add(new ExecutionStep
            {
                StepNumber = 7,
                Description = "Verify CI/CD pipeline passes",
                Operation = "scm.verify_pipeline"
            });
        }

        // Create summary
        var summary = GeneratePlanSummary(requirements, selectedTemplate, plannedFiles, repositoryDetails, pipelineDefinition);

        var plan = new ExecutionPlan
        {
            Requirements = requirements,
            Template = selectedTemplate,
            PlannedFiles = plannedFiles.ToList(),
            Repository = repositoryDetails,
            Pipeline = pipelineDefinition,
            PolicyResults = policyResults,
            Summary = summary,
            Steps = steps
        };

        await _auditLogger.LogActionAsync(
            "agent.create_plan",
            ExecutionPhase.Planning,
            ActionResult.Success,
            $"Plan created with {steps.Count} steps");

        return plan;
    }

    public async Task<DeliveryResult> ExecutePlanAsync(
        ExecutionPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (!plan.IsApproved)
        {
            throw new InvalidOperationException("Plan must be approved before execution");
        }

        _logger.LogInformation("Executing plan for project: {ProjectName}", plan.Requirements.ProjectName);

        await _auditLogger.LogActionAsync(
            "agent.execute_plan",
            ExecutionPhase.Execution,
            ActionResult.Pending,
            $"Executing plan for {plan.Requirements.ProjectName}");

        string? workspacePath = null;

        try
        {
            // Step 1: Create workspace and scaffold
            workspacePath = await _filesystemOperations.CreateWorkspaceAsync(plan.Requirements.ProjectName, cancellationToken);
            await ExecuteStepAsync(plan.Steps[1], async () =>
            {
                var result = await _templateEngine.ScaffoldProjectAsync(
                    plan.Template,
                    plan.Requirements,
                    workspacePath,
                    cancellationToken);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Scaffolding failed: {result.ErrorMessage}");
                }
            }, cancellationToken);

            // Generate pipeline file if needed
            if (plan.Pipeline != null)
            {
                var pipelineGenerator = _pipelineGenerators.First(g =>
                    g.Platform == plan.Requirements.ScmPlatform);

                var pipelineResult = await pipelineGenerator.GeneratePipelineAsync(plan.Requirements, cancellationToken);
                var pipelineFilePath = Path.Combine(workspacePath, pipelineResult.FilePath);
                var pipelineDir = Path.GetDirectoryName(pipelineFilePath);

                if (!string.IsNullOrEmpty(pipelineDir))
                {
                    await _filesystemOperations.CreateDirectoryAsync(pipelineDir, cancellationToken);
                }

                await _filesystemOperations.WriteFileAsync(pipelineFilePath, pipelineResult.Content, cancellationToken);
            }

            // Step 2: Create remote repository
            RepositoryDetails? createdRepo = null;
            await ExecuteStepAsync(plan.Steps[2], async () =>
            {
                createdRepo = await _sourceControlProvider.CreateRepositoryAsync(
                    plan.Requirements.ProjectName,
                    plan.Requirements.Description,
                    plan.Requirements.Visibility,
                    cancellationToken);

                plan.Repository.Url = createdRepo.Url;
                plan.Repository.CloneUrl = createdRepo.CloneUrl;
            }, cancellationToken);

            // Step 3: Initialize Git and commit
            await ExecuteStepAsync(plan.Steps[3], async () =>
            {
                await _gitOperations.InitializeRepositoryAsync(workspacePath, _config.Common.DefaultBranch, cancellationToken);
                await _gitOperations.StageAllAsync(workspacePath, cancellationToken);
                await _gitOperations.CommitAsync(
                    workspacePath,
                    "Initial commit — scaffolded by CRISP",
                    "CRISP Agent",
                    "crisp@scaffold.local",
                    cancellationToken);
            }, cancellationToken);

            // Step 4: Push to remote
            await ExecuteStepAsync(plan.Steps[4], async () =>
            {
                await _gitOperations.AddRemoteAsync(
                    workspacePath,
                    "origin",
                    plan.Repository.CloneUrl!,
                    cancellationToken);

                var credentials = GetGitCredentials();
                await _gitOperations.PushAsync(
                    workspacePath,
                    "origin",
                    _config.Common.DefaultBranch,
                    credentials,
                    cancellationToken);
            }, cancellationToken);

            // Step 5-6: Trigger and verify pipeline
            string? pipelineUrl = null;
            string? buildStatus = null;

            if (plan.Pipeline != null && plan.Steps.Count > 5)
            {
                await ExecuteStepAsync(plan.Steps[5], async () =>
                {
                    var (runUrl, runId) = await _sourceControlProvider.TriggerPipelineAsync(
                        plan.Requirements.ProjectName,
                        null,
                        _config.Common.DefaultBranch,
                        cancellationToken);
                    pipelineUrl = runUrl;

                    // Wait and verify
                    for (var attempt = 0; attempt < MaxRemediationAttempts; attempt++)
                    {
                        await Task.Delay(10000, cancellationToken); // Wait 10 seconds

                        var (status, conclusion) = await _sourceControlProvider.GetPipelineStatusAsync(
                            plan.Requirements.ProjectName,
                            runId,
                            cancellationToken);

                        if (status == "completed" || status == "finished")
                        {
                            buildStatus = conclusion ?? status;
                            if (conclusion is "success" or "succeeded")
                            {
                                break;
                            }
                        }
                    }
                }, cancellationToken);
            }

            // Generate delivery result
            var vsCodeLink = GenerateVsCodeLink(plan.Repository.Url!, plan.Requirements.ScmPlatform);

            var deliveryResult = new DeliveryResult
            {
                Success = true,
                Platform = plan.Requirements.ScmPlatform.ToString(),
                RepositoryUrl = plan.Repository.Url!,
                CloneUrl = plan.Repository.CloneUrl!,
                DefaultBranch = _config.Common.DefaultBranch,
                PipelineUrl = pipelineUrl,
                BuildStatus = buildStatus ?? "N/A",
                VsCodeLink = vsCodeLink,
                CollectionUrl = plan.Requirements.ScmPlatform == ScmPlatform.AzureDevOps
                    ? $"{_config.AzureDevOps.ServerUrl}/{_config.AzureDevOps.Collection}"
                    : null,
                ProjectName = plan.Requirements.ScmPlatform == ScmPlatform.AzureDevOps
                    ? _config.AzureDevOps.Project
                    : null,
                SummaryCard = GenerateSummaryCard(plan, pipelineUrl, buildStatus, vsCodeLink)
            };

            await _auditLogger.LogActionAsync(
                "agent.execute_plan",
                ExecutionPhase.Delivery,
                ActionResult.Success,
                $"Repository created: {deliveryResult.RepositoryUrl}");

            return deliveryResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan execution failed");

            await _auditLogger.LogActionAsync(
                "agent.execute_plan",
                ExecutionPhase.Execution,
                ActionResult.Failure,
                $"Execution failed: {ex.Message}");

            return new DeliveryResult
            {
                Success = false,
                Platform = plan.Requirements.ScmPlatform.ToString(),
                RepositoryUrl = string.Empty,
                CloneUrl = string.Empty,
                DefaultBranch = _config.Common.DefaultBranch,
                VsCodeLink = string.Empty,
                ErrorMessage = ex.Message,
                SummaryCard = $"❌ Execution failed: {ex.Message}"
            };
        }
        finally
        {
            // Cleanup workspace
            if (workspacePath != null)
            {
                try
                {
                    await _filesystemOperations.CleanupWorkspaceAsync(workspacePath, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup workspace");
                }
            }
        }
    }

    public async Task<DeliveryResult> ScaffoldProjectAsync(
        ProjectRequirements requirements,
        bool autoApprove = false,
        CancellationToken cancellationToken = default)
    {
        var plan = await CreatePlanAsync(requirements, cancellationToken);

        if (!autoApprove)
        {
            PlanReadyForApproval?.Invoke(this, new ExecutionPlanEventArgs { Plan = plan });
            // In a real implementation, wait for approval
        }

        plan.IsApproved = true;
        return await ExecutePlanAsync(plan, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validate platform-specific configuration
        if (_config.ScmPlatform == ScmPlatform.GitHub)
        {
            if (string.IsNullOrEmpty(_config.GitHub.Owner))
            {
                errors.Add("GitHub owner is not configured");
            }

            if (string.IsNullOrEmpty(_config.GitHub.Token))
            {
                errors.Add("GitHub token is not configured");
            }
        }
        else
        {
            if (string.IsNullOrEmpty(_config.AzureDevOps.ServerUrl))
            {
                errors.Add("Azure DevOps Server URL is not configured");
            }

            if (string.IsNullOrEmpty(_config.AzureDevOps.Token))
            {
                errors.Add("Azure DevOps token is not configured");
            }
        }

        // Validate connection
        if (errors.Count == 0)
        {
            var connectionValid = await _sourceControlProvider.ValidateConnectionAsync(cancellationToken);
            if (!connectionValid)
            {
                errors.Add("Failed to connect to source control platform");
            }
        }

        return errors;
    }

    private async Task ExecuteStepAsync(
        ExecutionStep step,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing step {StepNumber}: {Description}", step.StepNumber, step.Description);

        try
        {
            await action();
            step.IsCompleted = true;
            step.Result = "Success";

            StepCompleted?.Invoke(this, new ExecutionStepEventArgs
            {
                Step = step,
                Success = true
            });

            await _auditLogger.LogActionAsync(
                step.Operation,
                ExecutionPhase.Execution,
                ActionResult.Success,
                step.Description);
        }
        catch (Exception ex)
        {
            step.Result = $"Failed: {ex.Message}";

            StepCompleted?.Invoke(this, new ExecutionStepEventArgs
            {
                Step = step,
                Success = false,
                ErrorMessage = ex.Message
            });

            await _auditLogger.LogActionAsync(
                step.Operation,
                ExecutionPhase.Execution,
                ActionResult.Failure,
                ex.Message);

            throw;
        }
    }

    private GitCredentials GetGitCredentials()
    {
        if (_config.ScmPlatform == ScmPlatform.GitHub)
        {
            return new GitCredentials
            {
                Username = _config.GitHub.Owner,
                Password = _config.GitHub.Token ?? string.Empty
            };
        }

        return new GitCredentials
        {
            Username = "pat",
            Password = _config.AzureDevOps.Token ?? string.Empty
        };
    }

    private static string GeneratePlanSummary(
        ProjectRequirements requirements,
        TemplateSelection template,
        IReadOnlyList<PlannedFile> files,
        RepositoryDetails repo,
        PipelineDefinition? pipeline)
    {
        var summary = $"""
            Execution Plan for: {requirements.ProjectName}

            1. Template: {template.Name} v{template.Version}
            2. Files to create: {files.Count} files/directories
            3. Repository: {repo.Owner}/{requirements.ProjectName} ({repo.Visibility})
            4. Branch: {repo.DefaultBranch}
            """;

        if (pipeline != null)
        {
            summary += $"""

            5. CI/CD: {pipeline.FileName}
               Steps: {string.Join(" → ", pipeline.BuildSteps)}
            """;
        }

        return summary;
    }

    private string GenerateSummaryCard(
        ExecutionPlan plan,
        string? pipelineUrl,
        string? buildStatus,
        string vsCodeLink)
    {
        if (plan.Requirements.ScmPlatform == ScmPlatform.GitHub)
        {
            return $"""
                ✅ Repository ready!

                Platform    : GitHub
                Repository  : {plan.Repository.Url}
                Branch      : {_config.Common.DefaultBranch}
                CI Workflow : {pipelineUrl ?? "N/A"}
                Build status: {(buildStatus == "success" ? "✅ Passing" : buildStatus ?? "N/A")}

                🔗 Open in VS Code: {vsCodeLink}
                """;
        }

        return $"""
            ✅ Repository ready!

            Platform    : Azure DevOps Server (on-prem)
            Collection  : {_config.AzureDevOps.ServerUrl}/{_config.AzureDevOps.Collection}
            Project     : {_config.AzureDevOps.Project}
            Repository  : {plan.Repository.Url}
            Branch      : {_config.Common.DefaultBranch}
            Pipeline    : {pipelineUrl ?? "N/A"}
            Build status: {(buildStatus is "succeeded" or "success" ? "✅ Passing" : buildStatus ?? "N/A")}

            🔗 Open in VS Code: {vsCodeLink}
            """;
    }

    private static string GenerateVsCodeLink(string repositoryUrl, ScmPlatform platform)
    {
        // For GitHub repositories, use vscode.dev which opens instantly in browser
        // Format: https://vscode.dev/github/owner/repo
        if (platform == ScmPlatform.GitHub)
        {
            // Extract owner/repo from GitHub URL
            // e.g., https://github.com/owner/repo -> owner/repo
            var uri = new Uri(repositoryUrl);
            var path = uri.AbsolutePath.TrimStart('/').TrimEnd('/');
            if (path.EndsWith(".git"))
            {
                path = path[..^4];
            }
            return $"https://vscode.dev/github/{path}";
        }

        // For Azure DevOps, use the web-based editor
        // Format: https://dev.azure.com/org/project/_git/repo?path=/&version=GBmain&_a=contents
        // The repository URL is already the web URL, just append editor parameters
        if (repositoryUrl.Contains("dev.azure.com") || repositoryUrl.Contains("visualstudio.com"))
        {
            return $"{repositoryUrl}?path=/&_a=contents";
        }

        // Fallback: return the repository URL itself
        return repositoryUrl;
    }
}
