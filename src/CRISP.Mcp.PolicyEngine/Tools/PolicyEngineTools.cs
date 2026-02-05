using System.ComponentModel;
using CRISP.Core.Interfaces;
using ModelContextProtocol.Server;

namespace CRISP.Mcp.PolicyEngine.Tools;

/// <summary>
/// MCP tools for policy engine operations.
/// </summary>
[McpServerToolType]
public sealed class PolicyEngineTools
{
    private readonly IPolicyEngine _policyEngine;

    public PolicyEngineTools(IPolicyEngine policyEngine)
    {
        _policyEngine = policyEngine;
    }

    [McpServerTool(Name = "get_policies")]
    [Description("Gets all loaded policy definitions")]
    public GetPoliciesResult GetPolicies()
    {
        var policies = _policyEngine.GetPolicies();

        return new GetPoliciesResult
        {
            Count = policies.Count,
            Policies = policies.Select(p => new PolicySummary
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Severity = p.Severity,
                Category = p.Category,
                Enabled = p.Enabled
            }).ToList()
        };
    }

    [McpServerTool(Name = "load_policies")]
    [Description("Loads policies from a JSON or YAML file")]
    public async Task<LoadPoliciesResult> LoadPoliciesAsync(
        [Description("Path to policy file (JSON or YAML)")] string policyPath)
    {
        await _policyEngine.LoadPoliciesAsync(policyPath);
        var policies = _policyEngine.GetPolicies();

        return new LoadPoliciesResult
        {
            Success = true,
            LoadedCount = policies.Count,
            Message = $"Loaded {policies.Count} policies from {policyPath}"
        };
    }

    [McpServerTool(Name = "check_policies_passed")]
    [Description("Checks if all policy validations passed")]
    public CheckPoliciesResult CheckPoliciesPassed(
        [Description("Policy results as JSON array")] string resultsJson)
    {
        var results = System.Text.Json.JsonSerializer.Deserialize<List<PolicyResultInput>>(
            resultsJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? [];

        var coreResults = results.Select(r => new CRISP.Core.Models.PolicyValidationResult
        {
            PolicyId = r.PolicyId,
            PolicyName = r.PolicyName,
            Passed = r.Passed,
            Message = r.Message,
            Severity = r.Severity
        }).ToList();

        var allPassed = _policyEngine.AllPoliciesPassed(coreResults);
        var failed = coreResults.Where(r => !r.Passed && r.Severity == "error").ToList();
        var warnings = coreResults.Where(r => !r.Passed && r.Severity == "warning").ToList();

        return new CheckPoliciesResult
        {
            AllPassed = allPassed,
            FailedCount = failed.Count,
            WarningCount = warnings.Count,
            FailedPolicies = failed.Select(f => f.PolicyName).ToList(),
            WarningPolicies = warnings.Select(w => w.PolicyName).ToList()
        };
    }
}

public sealed record GetPoliciesResult
{
    public required int Count { get; init; }
    public required IReadOnlyList<PolicySummary> Policies { get; init; }
}

public sealed record PolicySummary
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Severity { get; init; }
    public required string Category { get; init; }
    public required bool Enabled { get; init; }
}

public sealed record LoadPoliciesResult
{
    public required bool Success { get; init; }
    public required int LoadedCount { get; init; }
    public required string Message { get; init; }
}

public sealed record CheckPoliciesResult
{
    public required bool AllPassed { get; init; }
    public required int FailedCount { get; init; }
    public required int WarningCount { get; init; }
    public required IReadOnlyList<string> FailedPolicies { get; init; }
    public required IReadOnlyList<string> WarningPolicies { get; init; }
}

internal sealed record PolicyResultInput
{
    public required string PolicyId { get; init; }
    public required string PolicyName { get; init; }
    public required bool Passed { get; init; }
    public required string Message { get; init; }
    public string? Severity { get; init; }
}
