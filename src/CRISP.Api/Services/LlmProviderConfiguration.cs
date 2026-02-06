namespace CRISP.Api.Services;

/// <summary>
/// LLM provider type.
/// </summary>
public enum LlmProvider
{
    /// <summary>
    /// Anthropic Claude (default).
    /// </summary>
    Claude,

    /// <summary>
    /// OpenAI or OpenAI-compatible API.
    /// </summary>
    OpenAI
}

/// <summary>
/// Configuration for LLM provider selection.
/// </summary>
public sealed class LlmConfiguration
{
    public const string SectionName = "Llm";

    /// <summary>
    /// The LLM provider to use. Defaults to Claude.
    /// </summary>
    public LlmProvider Provider { get; set; } = LlmProvider.Claude;
}
