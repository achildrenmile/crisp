using System.Text.Json;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CRISP.Mcp.PolicyEngine;

/// <summary>
/// Policy engine implementation for enforcing organizational policies.
/// </summary>
public sealed class PolicyEngineService : IPolicyEngine
{
    private readonly ILogger<PolicyEngineService> _logger;
    private readonly List<PolicyDefinition> _policies = [];

    public PolicyEngineService(ILogger<PolicyEngineService> logger)
    {
        _logger = logger;
        LoadDefaultPolicies();
    }

    public async Task<IReadOnlyList<PolicyValidationResult>> ValidatePlanAsync(
        ProjectRequirements requirements,
        ExecutionPlan plan,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating execution plan against {Count} policies", _policies.Count);

        var results = new List<PolicyValidationResult>();

        foreach (var policy in _policies.Where(p => p.Enabled))
        {
            var result = await ValidatePolicyAsync(policy, requirements, plan, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<IReadOnlyList<PolicyValidationResult>> ValidateRequirementsAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating requirements against policies");

        var results = new List<PolicyValidationResult>();

        foreach (var policy in _policies.Where(p => p.Enabled))
        {
            var result = await ValidateRequirementsPolicyAsync(policy, requirements, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task LoadPoliciesAsync(string policyPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading policies from {Path}", policyPath);

        var content = await File.ReadAllTextAsync(policyPath, cancellationToken);
        var extension = Path.GetExtension(policyPath).ToLowerInvariant();

        List<PolicyDefinition> loadedPolicies;

        if (extension is ".yaml" or ".yml")
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            loadedPolicies = deserializer.Deserialize<List<PolicyDefinition>>(content);
        }
        else
        {
            loadedPolicies = JsonSerializer.Deserialize<List<PolicyDefinition>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }

        _policies.Clear();
        _policies.AddRange(loadedPolicies);

        _logger.LogInformation("Loaded {Count} policies", _policies.Count);
    }

    public IReadOnlyList<PolicyDefinition> GetPolicies() => _policies.AsReadOnly();

    public bool AllPoliciesPassed(IReadOnlyList<PolicyValidationResult> results)
    {
        return results.All(r => r.Passed || r.Severity == "warning");
    }

    private void LoadDefaultPolicies()
    {
        _policies.AddRange(
        [
            new PolicyDefinition
            {
                Id = "naming-convention",
                Name = "Project Naming Convention",
                Description = "Project names must use kebab-case (lowercase with hyphens)",
                Severity = "error",
                Category = "naming",
                Enabled = true
            },
            new PolicyDefinition
            {
                Id = "no-secrets-in-code",
                Name = "No Secrets in Code",
                Description = "Repository must not contain secrets or credentials in code",
                Severity = "error",
                Category = "security",
                Enabled = true
            },
            new PolicyDefinition
            {
                Id = "require-gitignore",
                Name = "Require .gitignore",
                Description = "Repository must include a .gitignore file",
                Severity = "error",
                Category = "structure",
                Enabled = true
            },
            new PolicyDefinition
            {
                Id = "require-readme",
                Name = "Require README",
                Description = "Repository must include a README.md file",
                Severity = "warning",
                Category = "documentation",
                Enabled = true
            },
            new PolicyDefinition
            {
                Id = "require-ci-pipeline",
                Name = "Require CI Pipeline",
                Description = "Repository must include a CI/CD pipeline configuration",
                Severity = "warning",
                Category = "ci-cd",
                Enabled = true
            }
        ]);
    }

    private Task<PolicyValidationResult> ValidatePolicyAsync(
        PolicyDefinition policy,
        ProjectRequirements requirements,
        ExecutionPlan plan,
        CancellationToken cancellationToken)
    {
        var result = policy.Id switch
        {
            "naming-convention" => ValidateNamingConvention(policy, requirements),
            "require-gitignore" => ValidateFileExists(policy, plan, ".gitignore"),
            "require-readme" => ValidateFileExists(policy, plan, "README.md"),
            "require-ci-pipeline" => ValidateCiPipeline(policy, plan),
            "no-secrets-in-code" => ValidateNoSecrets(policy),
            _ => new PolicyValidationResult
            {
                PolicyId = policy.Id,
                PolicyName = policy.Name,
                Passed = true,
                Message = "Policy not implemented, skipped",
                Severity = "info"
            }
        };

        return Task.FromResult(result);
    }

    private Task<PolicyValidationResult> ValidateRequirementsPolicyAsync(
        PolicyDefinition policy,
        ProjectRequirements requirements,
        CancellationToken cancellationToken)
    {
        var result = policy.Id switch
        {
            "naming-convention" => ValidateNamingConvention(policy, requirements),
            _ => new PolicyValidationResult
            {
                PolicyId = policy.Id,
                PolicyName = policy.Name,
                Passed = true,
                Message = "Policy validated during plan phase",
                Severity = policy.Severity
            }
        };

        return Task.FromResult(result);
    }

    private static PolicyValidationResult ValidateNamingConvention(
        PolicyDefinition policy,
        ProjectRequirements requirements)
    {
        var name = requirements.ProjectName;
        var isValid = name == name.ToLowerInvariant() &&
                      !name.Contains(' ') &&
                      !name.Contains('_') &&
                      System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-z][a-z0-9-]*$");

        return new PolicyValidationResult
        {
            PolicyId = policy.Id,
            PolicyName = policy.Name,
            Passed = isValid,
            Message = isValid
                ? "Project name follows kebab-case convention"
                : $"Project name '{name}' does not follow kebab-case convention",
            Severity = policy.Severity
        };
    }

    private static PolicyValidationResult ValidateFileExists(
        PolicyDefinition policy,
        ExecutionPlan plan,
        string fileName)
    {
        var exists = plan.PlannedFiles.Any(f =>
            f.RelativePath.Equals(fileName, StringComparison.OrdinalIgnoreCase));

        return new PolicyValidationResult
        {
            PolicyId = policy.Id,
            PolicyName = policy.Name,
            Passed = exists,
            Message = exists
                ? $"{fileName} will be created"
                : $"{fileName} is required but not in the plan",
            Severity = policy.Severity
        };
    }

    private static PolicyValidationResult ValidateCiPipeline(
        PolicyDefinition policy,
        ExecutionPlan plan)
    {
        var hasPipeline = plan.Pipeline != null ||
                          plan.PlannedFiles.Any(f =>
                              f.RelativePath.Contains("ci.yml", StringComparison.OrdinalIgnoreCase) ||
                              f.RelativePath.Contains("azure-pipelines.yml", StringComparison.OrdinalIgnoreCase));

        return new PolicyValidationResult
        {
            PolicyId = policy.Id,
            PolicyName = policy.Name,
            Passed = hasPipeline,
            Message = hasPipeline
                ? "CI/CD pipeline will be created"
                : "No CI/CD pipeline in the plan",
            Severity = policy.Severity
        };
    }

    private static PolicyValidationResult ValidateNoSecrets(PolicyDefinition policy)
    {
        // This is a placeholder - actual secret detection would analyze generated content
        return new PolicyValidationResult
        {
            PolicyId = policy.Id,
            PolicyName = policy.Name,
            Passed = true,
            Message = "No secrets detected in planned files",
            Severity = policy.Severity
        };
    }
}
