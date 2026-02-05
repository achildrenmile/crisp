using CRISP.Api.Models;

namespace CRISP.Api.Services;

/// <summary>
/// Chat-based agent for processing developer messages.
/// </summary>
public interface IChatAgent
{
    /// <summary>
    /// Processes a user message and generates a response.
    /// </summary>
    /// <param name="session">The chat session.</param>
    /// <param name="userMessage">The user's message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's response.</returns>
    Task<ChatMessage> ProcessMessageAsync(
        CrispSession session,
        string userMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles plan approval or rejection.
    /// </summary>
    /// <param name="session">The chat session.</param>
    /// <param name="approved">Whether the plan was approved.</param>
    /// <param name="feedback">Optional feedback for adjustments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's response.</returns>
    Task<ChatMessage> HandleApprovalAsync(
        CrispSession session,
        bool approved,
        string? feedback,
        CancellationToken cancellationToken = default);
}
