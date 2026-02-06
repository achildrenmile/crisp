namespace CRISP.Api.Services;

/// <summary>
/// Generic interface for interacting with LLM providers.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Gets information about the currently configured LLM provider and model.
    /// </summary>
    LlmInfo GetInfo();

    /// <summary>
    /// Sends a message to the LLM and gets a response.
    /// </summary>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="conversationHistory">Previous messages in the conversation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The LLM's response text.</returns>
    Task<string> SendMessageAsync(
        string systemPrompt,
        IReadOnlyList<(string Role, string Content)> conversationHistory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the LLM with streaming response.
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

/// <summary>
/// Information about the LLM provider and model.
/// </summary>
public sealed record LlmInfo(
    string Provider,
    string Model,
    string? BaseUrl = null);
