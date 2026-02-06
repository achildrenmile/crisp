using CRISP.Core.Configuration;
using CRISP.Core.Models;

namespace CRISP.Api.Models;

/// <summary>
/// Request to create a new chat session.
/// </summary>
public sealed record CreateSessionRequest(CrispConfiguration? Configuration = null);

/// <summary>
/// Response after creating a session.
/// </summary>
public sealed record CreateSessionResponse(
    string SessionId,
    string Status,
    DateTime CreatedAt);

/// <summary>
/// Request to send a message to the agent.
/// </summary>
public sealed record SendMessageRequest(string Content);

/// <summary>
/// Response after sending a message.
/// </summary>
public sealed record SendMessageResponse(string MessageId, string Role, string Content, DateTime Timestamp);

/// <summary>
/// Request to approve or reject the execution plan.
/// </summary>
public sealed record ApproveRequest(bool Approved, string? Feedback = null);

/// <summary>
/// A chat message in the conversation.
/// </summary>
public sealed record ChatMessage(
    string MessageId,
    string Role,
    string Content,
    DateTime Timestamp,
    MessageMetadata? Metadata = null);

/// <summary>
/// Metadata attached to chat messages.
/// </summary>
public sealed record MessageMetadata(
    string? Phase = null,
    ExecutionPlanDto? Plan = null,
    DeliveryCardDto? DeliveryCard = null);

/// <summary>
/// Simplified execution plan for API responses.
/// </summary>
public sealed record ExecutionPlanDto(
    Guid Id,
    List<PlanStepDto> Steps,
    List<PolicyResultDto> PolicyResults,
    string Summary);

/// <summary>
/// A step in the execution plan.
/// </summary>
public sealed record PlanStepDto(
    int Number,
    string Description,
    string Status);

/// <summary>
/// A policy validation result.
/// </summary>
public sealed record PolicyResultDto(
    string Rule,
    bool Passed,
    string? Detail = null);

/// <summary>
/// Delivery card showing the final result.
/// </summary>
public sealed record DeliveryCardDto(
    string Platform,
    string RepositoryUrl,
    string Branch,
    string? PipelineUrl,
    string BuildStatus,
    string VsCodeWebUrl,
    string VsCodeCloneUrl);

/// <summary>
/// Session status response.
/// </summary>
public sealed record SessionStatusResponse(
    string SessionId,
    SessionStatus Status,
    int MessageCount,
    DateTime CreatedAt,
    DateTime? LastActivityAt);

/// <summary>
/// Session history item for the history list.
/// </summary>
public sealed record SessionHistoryItem(
    string SessionId,
    string? ProjectName,
    string Status,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    string? RepositoryUrl,
    string? VsCodeWebUrl,
    string? VsCodeCloneUrl,
    string? FirstMessage);

/// <summary>
/// The possible states of a chat session.
/// </summary>
public enum SessionStatus
{
    Intake,
    Planning,
    AwaitingApproval,
    Executing,
    Delivering,
    Completed,
    Failed
}
