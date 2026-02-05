namespace CRISP.Api.Services;

/// <summary>
/// Client for interacting with Claude API.
/// </summary>
public interface IClaudeClient
{
    /// <summary>
    /// Sends a message to Claude and gets a response.
    /// </summary>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="conversationHistory">Previous messages in the conversation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Claude's response text.</returns>
    Task<string> SendMessageAsync(
        string systemPrompt,
        IReadOnlyList<(string Role, string Content)> conversationHistory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to Claude with streaming response.
    /// </summary>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="conversationHistory">Previous messages in the conversation.</param>
    /// <param name="onChunk">Callback for each text chunk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete response text.</returns>
    Task<string> SendMessageStreamingAsync(
        string systemPrompt,
        IReadOnlyList<(string Role, string Content)> conversationHistory,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default);
}
