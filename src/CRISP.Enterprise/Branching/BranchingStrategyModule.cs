using CRISP.Adr;
using Microsoft.Extensions.Logging;

namespace CRISP.Enterprise.Branching;

/// <summary>
/// Documents the branching strategy and configures branch protection rules.
/// </summary>
public sealed class BranchingStrategyModule : IEnterpriseModule
{
    private readonly ILogger<BranchingStrategyModule> _logger;

    public BranchingStrategyModule(ILogger<BranchingStrategyModule> logger)
    {
        _logger = logger;
    }

    public string Id => "branching-strategy";
    public string DisplayName => "Branching Strategy";
    public int Order => 500;

    public bool ShouldRun(ProjectContext context) => true;

    public async Task<ModuleResult> ExecuteAsync(ProjectContext context, CancellationToken cancellationToken = default)
    {
        var filesCreated = new List<string>();
        var scmConfigActions = new List<string>();

        try
        {
            // Create docs directory
            var docsDir = Path.Combine(context.WorkspacePath, "docs");
            Directory.CreateDirectory(docsDir);

            // Generate BRANCHING.md
            var branchingPath = Path.Combine(docsDir, "BRANCHING.md");
            var branchingContent = GenerateBranchingMd(context);
            await File.WriteAllTextAsync(branchingPath, branchingContent, cancellationToken);
            filesCreated.Add("docs/BRANCHING.md");

            // Note: Actual branch protection is configured via SCM API after push
            scmConfigActions.Add($"branch-protection:{context.DefaultBranch}:documented");

            // Record ADR
            var strategyName = GetStrategyDisplayName(context.BranchingStrategy);
            context.DecisionCollector.Record(
                title: $"Adopt {strategyName} branching strategy",
                context: "Teams need a clear, consistent branching strategy to coordinate development and releases.",
                decision: $"Use {strategyName} with branch protection on `{context.DefaultBranch}`.",
                rationale: GetStrategyRationale(context.BranchingStrategy),
                category: AdrCategory.Development,
                alternatives: new Dictionary<string, string>
                {
                    ["Trunk-based"] = context.BranchingStrategy == "trunk-based"
                        ? "Selected for simplicity and CI/CD alignment"
                        : "Simpler but requires strong CI/CD discipline",
                    ["GitHub Flow"] = context.BranchingStrategy == "github-flow"
                        ? "Selected for simplicity with PR-based workflow"
                        : "Simple but lacks release branch structure",
                    ["GitFlow"] = context.BranchingStrategy == "gitflow"
                        ? "Selected for structured releases"
                        : "More complex but good for scheduled releases"
                },
                consequences: [
                    $"All developers follow {strategyName} workflow",
                    $"Branch protection enforces code review on `{context.DefaultBranch}`",
                    "CI must pass before merging"
                ],
                relatedFiles: filesCreated
            );

            return new ModuleResult
            {
                ModuleId = Id,
                Success = true,
                FilesCreated = filesCreated,
                ScmConfigActions = scmConfigActions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Branching strategy module failed");
            return ModuleResult.Failed(Id, ex.Message);
        }
    }

    private static string GenerateBranchingMd(ProjectContext context)
    {
        var strategy = context.BranchingStrategy.ToLowerInvariant();

        return strategy switch
        {
            "trunk-based" => GenerateTrunkBasedDoc(context),
            "github-flow" => GenerateGitHubFlowDoc(context),
            "gitflow" => GenerateGitFlowDoc(context),
            _ => GenerateTrunkBasedDoc(context)
        };
    }

    private static string GenerateTrunkBasedDoc(ProjectContext context) => $$"""
        # Branching Strategy: Trunk-Based Development

        ## Overview

        This project follows **Trunk-Based Development**, where all developers commit to a single
        branch (`{{context.DefaultBranch}}`) either directly or via short-lived feature branches.

        Trunk-based development enables continuous integration and continuous delivery by keeping
        the main branch always deployable. Feature flags are used to hide incomplete work.

        ## Branch Naming

        | Branch Type | Pattern | Example | Lifetime |
        |-------------|---------|---------|----------|
        | Main | `{{context.DefaultBranch}}` | `{{context.DefaultBranch}}` | Permanent |
        | Feature | `feature/{description}` | `feature/add-auth` | Hours to days |
        | Bugfix | `fix/{description}` | `fix/null-check` | Hours |
        | Release | `release/{version}` | `release/1.2.0` | Days (if needed) |

        ## Workflow

        1. **Start work**: Create a feature branch from `{{context.DefaultBranch}}`
           ```bash
           git checkout {{context.DefaultBranch}}
           git pull origin {{context.DefaultBranch}}
           git checkout -b feature/my-feature
           ```

        2. **Develop**: Make small, focused commits
           ```bash
           git add .
           git commit -m "feat: add user validation"
           ```

        3. **Stay current**: Rebase frequently to avoid divergence
           ```bash
           git fetch origin
           git rebase origin/{{context.DefaultBranch}}
           ```

        4. **Open PR**: Push and create pull request
           ```bash
           git push -u origin feature/my-feature
           ```

        5. **Review & merge**: After approval and CI passes, squash merge to `{{context.DefaultBranch}}`

        6. **Delete branch**: Clean up after merge

        ## Branch Protection Rules

        The following rules are enforced on `{{context.DefaultBranch}}`:

        - ✅ Require pull request before merging
        - ✅ Require at least 1 approval
        - ✅ Require status checks to pass (CI)
        - ✅ Dismiss stale reviews on new commits
        - ✅ No direct pushes

        ## Feature Flags

        For incomplete features that need to be merged:

        1. Wrap new code in feature flags
        2. Merge to `{{context.DefaultBranch}}` with flag disabled
        3. Enable flag when feature is complete
        4. Remove flag after feature is stable

        ---

        *This branching strategy was configured by [CRISP](https://github.com/strali/crisp).*
        """;

    private static string GenerateGitHubFlowDoc(ProjectContext context) => $$"""
        # Branching Strategy: GitHub Flow

        ## Overview

        This project follows **GitHub Flow**, a lightweight, branch-based workflow that supports
        continuous delivery. There's only one long-lived branch (`{{context.DefaultBranch}}`), and
        all work is done in feature branches.

        ## Branch Naming

        | Branch Type | Pattern | Example | Lifetime |
        |-------------|---------|---------|----------|
        | Main | `{{context.DefaultBranch}}` | `{{context.DefaultBranch}}` | Permanent |
        | Feature | `feature/{description}` | `feature/user-profile` | Days |
        | Bugfix | `fix/{description}` | `fix/login-error` | Hours to days |
        | Hotfix | `hotfix/{description}` | `hotfix/security-patch` | Hours |

        ## Workflow

        1. **Create a branch** from `{{context.DefaultBranch}}` with a descriptive name
           ```bash
           git checkout -b feature/add-notifications
           ```

        2. **Add commits** with clear messages

        3. **Open a Pull Request** early for discussion

        4. **Discuss and review** the code

        5. **Deploy** from the branch to staging for testing (optional)

        6. **Merge** to `{{context.DefaultBranch}}` after approval

        7. **Deploy** from `{{context.DefaultBranch}}` to production

        ## Branch Protection Rules

        The following rules are enforced on `{{context.DefaultBranch}}`:

        - ✅ Require pull request before merging
        - ✅ Require at least 1 approval
        - ✅ Require status checks to pass (CI)
        - ✅ No direct pushes
        - ✅ Require branches to be up to date

        ## Best Practices

        - Keep branches short-lived (merge within a few days)
        - Write descriptive PR titles and descriptions
        - Use draft PRs for work in progress
        - Squash merge to keep history clean

        ---

        *This branching strategy was configured by [CRISP](https://github.com/strali/crisp).*
        """;

    private static string GenerateGitFlowDoc(ProjectContext context) => """
        # Branching Strategy: GitFlow

        ## Overview

        This project follows **GitFlow**, a branching model designed for projects with scheduled
        releases. It uses multiple long-lived branches and structured feature/release branches.

        ## Branch Naming

        | Branch Type | Pattern | Example | Lifetime |
        |-------------|---------|---------|----------|
        | Main | `main` | `main` | Permanent |
        | Develop | `develop` | `develop` | Permanent |
        | Feature | `feature/{description}` | `feature/user-auth` | Days to weeks |
        | Release | `release/{version}` | `release/1.2.0` | Days |
        | Hotfix | `hotfix/{version}` | `hotfix/1.2.1` | Hours to days |

        ## Branch Structure

        ```
        main ─────●─────────────────●─────────── (production releases)
                  │                 │
        develop ──┼──●──●──●──●─────┼──●──●───── (integration branch)
                  │  │  │  │        │  │
        feature/a ───┘  │  │        │  │
        feature/b ──────┘  │        │  │
        release/1.0 ───────┘        │  │
        feature/c ──────────────────┘  │
        release/1.1 ───────────────────┘
        ```

        ## Workflow

        ### Feature Development

        1. Create feature branch from `develop`
           ```bash
           git checkout develop
           git checkout -b feature/my-feature
           ```

        2. Develop and commit

        3. Open PR to `develop`

        4. Merge after approval

        ### Creating a Release

        1. Create release branch from `develop`
           ```bash
           git checkout develop
           git checkout -b release/1.2.0
           ```

        2. Bump versions, final testing

        3. Merge to both `main` AND `develop`

        4. Tag `main` with version

        ### Hotfixes

        1. Create hotfix branch from `main`
           ```bash
           git checkout main
           git checkout -b hotfix/1.2.1
           ```

        2. Fix and test

        3. Merge to both `main` AND `develop`

        4. Tag `main` with version

        ## Branch Protection Rules

        **On `main`:**
        - ✅ Require pull request
        - ✅ Require 2 approvals
        - ✅ Require status checks (CI)
        - ✅ No direct pushes

        **On `develop`:**
        - ✅ Require pull request
        - ✅ Require 1 approval
        - ✅ Require status checks (CI)

        ---

        *This branching strategy was configured by [CRISP](https://github.com/strali/crisp).*
        """;

    private static string GetStrategyDisplayName(string strategy) => strategy.ToLowerInvariant() switch
    {
        "trunk-based" => "Trunk-Based Development",
        "github-flow" => "GitHub Flow",
        "gitflow" => "GitFlow",
        _ => "Trunk-Based Development"
    };

    private static string GetStrategyRationale(string strategy) => strategy.ToLowerInvariant() switch
    {
        "trunk-based" => "Trunk-based development reduces merge conflicts, enables continuous integration, and keeps the main branch always deployable. Ideal for teams practicing CI/CD.",
        "github-flow" => "GitHub Flow is simple and well-suited for web applications that deploy frequently. It avoids the complexity of GitFlow while maintaining PR-based code review.",
        "gitflow" => "GitFlow provides structure for scheduled releases with clear separation between development and production. Suitable for versioned software with multiple supported releases.",
        _ => "Trunk-based development is the default for its simplicity and CI/CD alignment."
    };
}
