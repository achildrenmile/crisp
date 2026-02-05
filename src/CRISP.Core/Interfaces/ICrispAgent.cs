using CRISP.Core.Configuration;
using CRISP.Core.Models;

namespace CRISP.Core.Interfaces;

/// <summary>
/// Main CRISP agent orchestrator interface.
/// </summary>
public interface ICrispAgent
{
    /// <summary>
    /// Processes developer requirements and generates an execution plan.
    /// </summary>
    /// <param name="requirements">Project requirements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated execution plan.</returns>
    Task<ExecutionPlan> CreatePlanAsync(
        ProjectRequirements requirements,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an approved plan.
    /// </summary>
    /// <param name="plan">Approved execution plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Delivery result.</returns>
    Task<DeliveryResult> ExecutePlanAsync(
        ExecutionPlan plan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Full orchestration: creates a plan, waits for approval, and executes.
    /// </summary>
    /// <param name="requirements">Project requirements.</param>
    /// <param name="autoApprove">Whether to auto-approve the plan (for testing).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Delivery result.</returns>
    Task<DeliveryResult> ScaffoldProjectAsync(
        ProjectRequirements requirements,
        bool autoApprove = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation errors, empty if valid.</returns>
    Task<IReadOnlyList<string>> ValidateConfigurationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current session identifier.
    /// </summary>
    Guid SessionId { get; }

    /// <summary>
    /// Event raised when a plan is ready for approval.
    /// </summary>
    event EventHandler<ExecutionPlanEventArgs>? PlanReadyForApproval;

    /// <summary>
    /// Event raised when an execution step completes.
    /// </summary>
    event EventHandler<ExecutionStepEventArgs>? StepCompleted;
}

/// <summary>
/// Event arguments for plan approval.
/// </summary>
public sealed class ExecutionPlanEventArgs : EventArgs
{
    /// <summary>
    /// The execution plan.
    /// </summary>
    public required ExecutionPlan Plan { get; init; }
}

/// <summary>
/// Event arguments for step completion.
/// </summary>
public sealed class ExecutionStepEventArgs : EventArgs
{
    /// <summary>
    /// The completed step.
    /// </summary>
    public required ExecutionStep Step { get; init; }

    /// <summary>
    /// Whether the step was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if the step failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
