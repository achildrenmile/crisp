using System.Text;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRISP.Api.Services;

/// <summary>
/// Configuration options for Claude API.
/// </summary>
public sealed class ClaudeApiOptions
{
    public const string SectionName = "Claude";

    /// <summary>
    /// API key for Anthropic Claude.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model to use (e.g., claude-sonnet-4-20250514).
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-20250514";

    /// <summary>
    /// Maximum tokens in the response.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;
}

/// <summary>
/// Claude API client implementation using Anthropic.SDK.
/// </summary>
public sealed class ClaudeClient : ILlmClient, IDisposable
{
    private readonly ILogger<ClaudeClient> _logger;
    private readonly ClaudeApiOptions _options;
    private readonly AnthropicClient _client;
    private bool _disposed;

    public ClaudeClient(
        ILogger<ClaudeClient> logger,
        IOptions<ClaudeApiOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _client = new AnthropicClient(_options.ApiKey);
    }

    public LlmInfo GetInfo() => new("Anthropic Claude", _options.Model);

    public async Task<string> SendMessageAsync(
        string systemPrompt,
        IReadOnlyList<(string Role, string Content)> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending message to Claude ({Model})", _options.Model);

        var messages = conversationHistory
            .Select(m => new Message
            {
                Role = m.Role == "user" ? RoleType.User : RoleType.Assistant,
                Content = [new TextContent { Text = m.Content }]
            })
            .ToList();

        var parameters = new MessageParameters
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            SystemMessage = systemPrompt,
            Messages = messages
        };

        var response = await ExecuteWithRetryAsync(
            () => _client.Messages.GetClaudeMessageAsync(parameters),
            cancellationToken);

        var responseText = response.Content
            .OfType<TextContent>()
            .Select(c => c.Text)
            .FirstOrDefault() ?? string.Empty;

        _logger.LogInformation("Received response from Claude ({Tokens} tokens)", response.Usage?.OutputTokens);

        return responseText;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken,
        int maxRetries = 3)
    {
        var delays = new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) };
        Exception? lastException = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                lastException = ex;
                var delay = delays[Math.Min(attempt, delays.Length - 1)];
                _logger.LogWarning(ex, "Claude API call failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s...",
                    attempt + 1, maxRetries + 1, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed after retries");
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("overloaded", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> SendMessageStreamingAsync(
        string systemPrompt,
        IReadOnlyList<(string Role, string Content)> conversationHistory,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending streaming message to Claude ({Model})", _options.Model);

        var messages = conversationHistory
            .Select(m => new Message
            {
                Role = m.Role == "user" ? RoleType.User : RoleType.Assistant,
                Content = [new TextContent { Text = m.Content }]
            })
            .ToList();

        var parameters = new MessageParameters
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            SystemMessage = systemPrompt,
            Messages = messages,
            Stream = true
        };

        var fullResponse = new StringBuilder();

        await foreach (var streamEvent in _client.Messages.StreamClaudeMessageAsync(parameters, cancellationToken))
        {
            // Extract text from streaming events
            if (streamEvent.Delta?.Text is string text && !string.IsNullOrEmpty(text))
            {
                fullResponse.Append(text);
                if (onChunk != null)
                {
                    await onChunk(text);
                }
            }
        }

        var responseText = fullResponse.ToString();
        _logger.LogInformation("Completed streaming response from Claude ({Length} chars)", responseText.Length);

        return responseText;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client.Dispose();
            _disposed = true;
        }
    }
}
