using CRISP.Core.Models;

namespace CRISP.Core.Interfaces;

/// <summary>
/// Policy engine for enforcing organizational policies.
/// </summary>
public interface IPolicyEngine
{
    /// <summary>
    /// Validates the execution plan against all configured policies.
    /// </summary>
    /// <param name="requirements">Project requirements.</param>
    /// <param name="plan">Execution plan to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of policy validation results.</returns>
    Task<IReadOnlyList<PolicyValidationResult>> ValidatePlanAsync(
        ProjectRequirements requirements,
        ExecutionPlan plan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates project requirements against policies before planning.
    /// </summary>
    /// <param name="requirements">Project requirements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of policy validation results.</returns>
    Task<IReadOnlyList<PolicyValidationResult>> ValidateRequirementsAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads policy rules from a file or configuration.
    /// </summary>
    /// <param name="policyPath">Path to policy file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LoadPoliciesAsync(
        string policyPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all loaded policies.
    /// </summary>
    /// <returns>List of policy definitions.</returns>
    IReadOnlyList<PolicyDefinition> GetPolicies();

    /// <summary>
    /// Checks if all policy validations passed.
    /// </summary>
    /// <param name="results">Policy validation results.</param>
    /// <returns>True if all policies passed.</returns>
    bool AllPoliciesPassed(IReadOnlyList<PolicyValidationResult> results);
}

/// <summary>
/// Definition of a policy rule.
/// </summary>
public sealed class PolicyDefinition
{
    /// <summary>
    /// Unique policy identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable policy name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Policy description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Severity level (error, warning, info).
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Policy category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Whether this policy is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Policy rule configuration.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Configuration { get; init; } =
        new Dictionary<string, object?>();
}
