using System.ClientModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace CRISP.Api.Services;

/// <summary>
/// Configuration options for OpenAI API.
/// </summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>
    /// API key for OpenAI.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model to use (e.g., gpt-4o, gpt-4-turbo).
    /// </summary>
    public string Model { get; set; } = "gpt-4o";

    /// <summary>
    /// Maximum tokens in the response.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Optional base URL for OpenAI-compatible APIs (e.g., Azure OpenAI, local LLMs).
    /// </summary>
    public string? BaseUrl { get; set; }
}

/// <summary>
/// OpenAI API client implementation.
/// </summary>
public sealed class OpenAiClient : ILlmClient
{
    private readonly ILogger<OpenAiClient> _logger;
    private readonly OpenAiOptions _options;
    private readonly ChatClient _client;

    public OpenAiClient(
        ILogger<OpenAiClient> logger,
        IOptions<OpenAiOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        OpenAIClientOptions? clientOptions = null;
        if (!string.IsNullOrEmpty(_options.BaseUrl))
        {
            clientOptions = new OpenAIClientOptions
            {
                Endpoint = new Uri(_options.BaseUrl)
            };
        }

        var apiKeyCredential = new ApiKeyCredential(_options.ApiKey);
        var openAiClient = new OpenAIClient(apiKeyCredential, clientOptions);
        _client = openAiClient.GetChatClient(_options.Model);
    }

    public LlmInfo GetInfo() => new(
        string.IsNullOrEmpty(_options.BaseUrl) ? "OpenAI" : "OpenAI-Compatible",
        _options.Model,
        _options.BaseUrl);

    public async Task<string> SendMessageAsync(
        string systemPrompt,
        IReadOnlyList<(string Role, string Content)> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending message to OpenAI ({Model})", _options.Model);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt)
        };

        foreach (var (role, content) in conversationHistory)
        {
            messages.Add(role == "user"
                ? new UserChatMessage(content)
                : new AssistantChatMessage(content));
        }

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _options.MaxTokens
        };

        var response = await _client.CompleteChatAsync(messages, chatOptions, cancellationToken);

        var responseText = response.Value.Content
            .Where(c => c.Kind == ChatMessageContentPartKind.Text)
            .Select(c => c.Text)
            .FirstOrDefault() ?? string.Empty;

        _logger.LogInformation("Received response from OpenAI ({Tokens} tokens)",
            response.Value.Usage?.OutputTokenCount);

        return responseText;
    }

    public async Task<string> SendMessageStreamingAsync(
        string systemPrompt,
        IReadOnlyList<(string Role, string Content)> conversationHistory,
        Func<string, Task>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending streaming message to OpenAI ({Model})", _options.Model);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt)
        };

        foreach (var (role, content) in conversationHistory)
        {
            messages.Add(role == "user"
                ? new UserChatMessage(content)
                : new AssistantChatMessage(content));
        }

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _options.MaxTokens
        };

        var fullResponse = new StringBuilder();

        await foreach (var update in _client.CompleteChatStreamingAsync(messages, chatOptions, cancellationToken))
        {
            foreach (var contentPart in update.ContentUpdate)
            {
                if (contentPart.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(contentPart.Text))
                {
                    fullResponse.Append(contentPart.Text);
                    if (onChunk != null)
                    {
                        await onChunk(contentPart.Text);
                    }
                }
            }
        }

        var responseText = fullResponse.ToString();
        _logger.LogInformation("Completed streaming response from OpenAI ({Length} chars)", responseText.Length);

        return responseText;
    }
}
