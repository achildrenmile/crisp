namespace CRISP.Api.Models;

/// <summary>
/// Base class for agent events sent via SSE.
/// </summary>
public abstract record AgentEvent(string Type, DateTime Timestamp);

/// <summary>
/// Agent sent a message.
/// </summary>
public sealed record AgentMessageEvent(
    string MessageId,
    string Content,
    DateTime Timestamp) : AgentEvent("agent_message", Timestamp);

/// <summary>
/// Execution plan is ready for review.
/// </summary>
public sealed record PlanReadyEvent(
    ExecutionPlanDto Plan,
    DateTime Timestamp) : AgentEvent("plan_ready", Timestamp);

/// <summary>
/// User approval is required before proceeding.
/// </summary>
public sealed record ApprovalRequiredEvent(
    string Message,
    DateTime Timestamp) : AgentEvent("approval_required", Timestamp);

/// <summary>
/// An execution step has started.
/// </summary>
public sealed record StepStartedEvent(
    int StepNumber,
    string Description,
    DateTime Timestamp) : AgentEvent("step_started", Timestamp);

/// <summary>
/// An execution step completed successfully.
/// </summary>
public sealed record StepCompletedEvent(
    int StepNumber,
    string Description,
    DateTime Timestamp) : AgentEvent("step_completed", Timestamp);

/// <summary>
/// An execution step failed.
/// </summary>
public sealed record StepFailedEvent(
    int StepNumber,
    string Description,
    string Error,
    DateTime Timestamp) : AgentEvent("step_failed", Timestamp);

/// <summary>
/// Build status update.
/// </summary>
public sealed record BuildStatusEvent(
    string Status,
    string? Conclusion,
    DateTime Timestamp) : AgentEvent("build_status", Timestamp);

/// <summary>
/// Delivery is ready - final result available.
/// </summary>
public sealed record DeliveryReadyEvent(
    DeliveryCardDto DeliveryCard,
    DateTime Timestamp) : AgentEvent("delivery_ready", Timestamp);

/// <summary>
/// An error occurred during processing.
/// </summary>
public sealed record ErrorEvent(
    string Message,
    DateTime Timestamp) : AgentEvent("error", Timestamp);
