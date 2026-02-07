using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.Sbom;

/// <summary>
/// Configures the CI/CD pipeline to generate a Software Bill of Materials (SBOM)
/// on every build for supply chain transparency and regulatory compliance.
/// </summary>
public sealed class SbomModule : IEnterpriseModule
{
    private readonly ILogger<SbomModule> _logger;

    public SbomModule(ILogger<SbomModule> logger)
    {
        _logger = logger;
    }

    public string Id => "sbom";
    public string DisplayName => "SBOM Configuration";
    public int Order => 200;

    public bool ShouldRun(ProjectContext context) => true; // Always runs

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        var filesCreated = new List<string>();
        var pipelineStepsAdded = new List<string>();

        try
        {
            // Create docs/sbom directory
            var sbomDocsDir = Path.Combine(context.WorkspacePath, "docs", "sbom");
            Directory.CreateDirectory(sbomDocsDir);

            // Generate SBOM README
            var readmePath = Path.Combine(sbomDocsDir, "README.md");
            var readmeContent = GenerateSbomReadme(context);
            await File.WriteAllTextAsync(readmePath, readmeContent, cancellationToken);
            filesCreated.Add("docs/sbom/README.md");

            // Add SBOM step to CI pipeline
            if (!string.IsNullOrEmpty(context.CiPipelineFile))
            {
                var pipelinePath = Path.Combine(context.WorkspacePath, context.CiPipelineFile);
                if (File.Exists(pipelinePath))
                {
                    var pipelineContent = await File.ReadAllTextAsync(pipelinePath, cancellationToken);
                    var (updatedContent, stepName) = AddSbomStep(pipelineContent, context);

                    if (updatedContent != pipelineContent)
                    {
                        await File.WriteAllTextAsync(pipelinePath, updatedContent, cancellationToken);
                        pipelineStepsAdded.Add(stepName);
                        _logger.LogInformation("Added SBOM step to pipeline: {StepName}", stepName);
                    }
                }
            }

            // Record ADR
            var format = context.SbomFormat.ToUpperInvariant() == "SPDX" ? "SPDX" : "CycloneDX";
            var tool = GetSbomTool(context);

            context.DecisionCollector.Record(
                title: $"Generate SBOM in CI pipeline using {format} format",
                context: "Regulatory requirements (EU CRA, US EO 14028) and enterprise procurement increasingly require Software Bills of Materials for supply chain transparency.",
                decision: $"Configure CI pipeline to generate SBOM in {format} format using {tool} on every build.",
                rationale: "Generating SBOM in CI ensures every release has an up-to-date component inventory. This supports vulnerability management, license compliance, and regulatory requirements.",
                category: AdrCategory.Compliance,
                alternatives: new Dictionary<string, string>
                {
                    ["Manual SBOM generation"] = "Rejected: manual process is error-prone and likely to become stale",
                    ["SPDX format"] = format == "CycloneDX" ? "CycloneDX chosen for broader tooling support" : "Selected for Linux Foundation backing",
                    ["CycloneDX format"] = format == "SPDX" ? "SPDX chosen for standards compliance" : "Selected for security-focused features"
                },
                consequences: [
                    "Every CI build produces an SBOM as a build artifact",
                    "Vulnerability scanners can cross-reference components with CVE databases",
                    "License compliance auditing is automated",
                    "Procurement teams can verify supply chain compliance"
                ],
                relatedFiles: filesCreated.Concat(pipelineStepsAdded.Select(_ => context.CiPipelineFile ?? "")).ToList()
            );

            return new ModuleResult
            {
                ModuleId = Id,
                Success = true,
                FilesCreated = filesCreated,
                PipelineStepsAdded = pipelineStepsAdded
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SBOM module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GenerateSbomReadme(ProjectContext context)
    {
        var format = context.SbomFormat.ToUpperInvariant() == "SPDX" ? "SPDX" : "CycloneDX";
        var tool = GetSbomTool(context);

        return $"""
            # Software Bill of Materials (SBOM)

            This project generates an SBOM automatically on every CI build.

            - **Format:** {format} (JSON)
            - **Tool:** {tool}
            - **Location:** Available as a build artifact in CI

            ## Why

            An SBOM provides a complete list of all components, libraries, and dependencies
            used in this software. This supports:

            - **Vulnerability management** — cross-reference with known CVEs
            - **License compliance auditing** — verify all dependencies meet license requirements
            - **Supply chain transparency** — full visibility into third-party components
            - **Regulatory compliance** — EU Cyber Resilience Act, US Executive Order 14028

            ## Viewing the SBOM

            1. Navigate to the CI/CD pipeline for the desired build
            2. Download the `sbom` artifact
            3. Open `sbom.json` in any SBOM viewer or text editor

            ## Tools for Analysis

            - [Dependency-Track](https://dependencytrack.org/) — vulnerability analysis platform
            - [Grype](https://github.com/anchore/grype) — vulnerability scanner for SBOMs
            - [SBOM Scorecard](https://github.com/eBay/sbom-scorecard) — SBOM quality checker

            ---

            *SBOM generation configured by [CRISP](https://github.com/strali/crisp).*
            """;
    }

    private static string GetSbomTool(ProjectContext context)
    {
        if (context.Language.Equals("csharp", StringComparison.OrdinalIgnoreCase))
        {
            return "Microsoft SBOM Tool";
        }

        return context.ScmPlatform.Equals("github", StringComparison.OrdinalIgnoreCase)
            ? "anchore/sbom-action"
            : "CycloneDX extension";
    }

    private static (string Content, string StepName) AddSbomStep(string pipelineContent, ProjectContext context)
    {
        if (context.ScmPlatform.Equals("github", StringComparison.OrdinalIgnoreCase))
        {
            return AddGitHubActionsSbomStep(pipelineContent, context);
        }
        else
        {
            return AddAzurePipelinesSbomStep(pipelineContent, context);
        }
    }

    private static (string Content, string StepName) AddGitHubActionsSbomStep(string content, ProjectContext context)
    {
        // Check if SBOM step already exists
        if (content.Contains("sbom-action") || content.Contains("Generate SBOM"))
        {
            return (content, "");
        }

        var format = context.SbomFormat.Equals("spdx", StringComparison.OrdinalIgnoreCase)
            ? "spdx-json"
            : "cyclonedx-json";

        var sbomStep = $"""

                  - name: Generate SBOM
                    uses: anchore/sbom-action@v0
                    with:
                      format: {format}
                      output-file: sbom.json

                  - name: Upload SBOM
                    uses: actions/upload-artifact@v4
                    with:
                      name: sbom
                      path: sbom.json
            """;

        // Find a good insertion point (before the last step or at the end of jobs.build.steps)
        var insertIndex = content.LastIndexOf("      - name:", StringComparison.Ordinal);
        if (insertIndex > 0)
        {
            // Find the end of the previous step block
            var nextStepIndex = content.IndexOf("\n      - name:", insertIndex + 1, StringComparison.Ordinal);
            if (nextStepIndex < 0)
            {
                // Last step, append after it
                var endOfSteps = content.LastIndexOf("\n", StringComparison.Ordinal);
                if (endOfSteps > 0)
                {
                    content = content.Insert(endOfSteps, sbomStep);
                }
            }
            else
            {
                content = content.Insert(nextStepIndex, sbomStep);
            }
        }

        return (content, "Generate SBOM");
    }

    private static (string Content, string StepName) AddAzurePipelinesSbomStep(string content, ProjectContext context)
    {
        // Check if SBOM step already exists
        if (content.Contains("CycloneDX") || content.Contains("sbom"))
        {
            return (content, "");
        }

        var sbomStep = """

              - task: CycloneDX@1
                displayName: 'Generate SBOM'
                inputs:
                  format: 'json'
                  outputPath: '$(Build.ArtifactStagingDirectory)/sbom.json'

              - task: PublishBuildArtifacts@1
                displayName: 'Publish SBOM'
                inputs:
                  PathtoPublish: '$(Build.ArtifactStagingDirectory)/sbom.json'
                  ArtifactName: 'sbom'
            """;

        // Append before the end of steps
        var stepsEndIndex = content.LastIndexOf("steps:", StringComparison.Ordinal);
        if (stepsEndIndex > 0)
        {
            var endOfFile = content.Length;
            content = content.Insert(endOfFile, sbomStep);
        }

        return (content, "Generate SBOM");
    }
}
