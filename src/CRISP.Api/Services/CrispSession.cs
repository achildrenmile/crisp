using System.Collections.Concurrent;
using System.Threading.Channels;
using CRISP.Api.Models;
using CRISP.Core.Configuration;
using CRISP.Core.Models;

namespace CRISP.Api.Services;

/// <summary>
/// Represents a CRISP chat session with conversation history and state.
/// </summary>
public sealed class CrispSession
{
    private readonly ConcurrentQueue<ChatMessage> _messages = new();
    private readonly Channel<AgentEvent> _eventChannel;

    public CrispSession(string sessionId, CrispConfiguration? configOverride = null)
    {
        SessionId = sessionId;
        Configuration = configOverride;
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = CreatedAt;
        Status = SessionStatus.Intake;

        _eventChannel = Channel.CreateUnbounded<AgentEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Optional configuration override for this session.
    /// </summary>
    public CrispConfiguration? Configuration { get; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// When the session was last active.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>
    /// Current session status.
    /// </summary>
    public SessionStatus Status { get; private set; }

    /// <summary>
    /// The execution plan, if one has been created.
    /// </summary>
    public ExecutionPlan? CurrentPlan { get; private set; }

    /// <summary>
    /// The delivery result, if execution completed.
    /// </summary>
    public DeliveryResult? DeliveryResult { get; private set; }

    /// <summary>
    /// Gets all messages in the conversation.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => _messages.ToList();

    /// <summary>
    /// Channel for reading agent events (for SSE).
    /// </summary>
    public ChannelReader<AgentEvent> EventReader => _eventChannel.Reader;

    /// <summary>
    /// Adds a user message to the conversation.
    /// </summary>
    public ChatMessage AddUserMessage(string content)
    {
        var message = new ChatMessage(
            MessageId: Guid.NewGuid().ToString(),
            Role: "user",
            Content: content,
            Timestamp: DateTime.UtcNow);

        _messages.Enqueue(message);
        LastActivityAt = DateTime.UtcNow;
        return message;
    }

    /// <summary>
    /// Adds an assistant message to the conversation.
    /// </summary>
    public ChatMessage AddAssistantMessage(string content, MessageMetadata? metadata = null)
    {
        var message = new ChatMessage(
            MessageId: Guid.NewGuid().ToString(),
            Role: "assistant",
            Content: content,
            Timestamp: DateTime.UtcNow,
            Metadata: metadata);

        _messages.Enqueue(message);
        LastActivityAt = DateTime.UtcNow;
        return message;
    }

    /// <summary>
    /// Publishes an event to connected clients.
    /// </summary>
    public async Task PublishEventAsync(AgentEvent evt)
    {
        await _eventChannel.Writer.WriteAsync(evt);
    }

    /// <summary>
    /// Updates the session status.
    /// </summary>
    public void SetStatus(SessionStatus status)
    {
        Status = status;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the current execution plan.
    /// </summary>
    public void SetPlan(ExecutionPlan plan)
    {
        CurrentPlan = plan;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the delivery result.
    /// </summary>
    public void SetDeliveryResult(DeliveryResult result)
    {
        DeliveryResult = result;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the conversation history in a format suitable for Claude API.
    /// </summary>
    public IReadOnlyList<(string Role, string Content)> GetConversationHistory()
    {
        return _messages
            .Select(m => (m.Role, m.Content))
            .ToList();
    }
}
