using CRISP.Api.Models;
using CRISP.Core.Configuration;
using CRISP.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRISP.Api.Services;

/// <summary>
/// Chat-based agent implementation that uses Claude for conversation.
/// For MVP Milestone 1, this handles simple chat without tool execution.
/// </summary>
public sealed class ChatAgent : IChatAgent
{
    private readonly ILogger<ChatAgent> _logger;
    private readonly IClaudeClient _claudeClient;
    private readonly ICrispAgent _crispAgent;
    private readonly CrispConfiguration _config;

    private const string SystemPrompt = """
        You are CRISP, an AI assistant that helps developers scaffold new code repositories.

        Your role is to:
        1. Understand what kind of project the developer wants to create
        2. Gather requirements through natural conversation
        3. Create an execution plan for scaffolding the repository
        4. Execute the plan after approval

        ## Project Types You Support:
        - ASP.NET Core Web API (.NET 8)
        - FastAPI (Python 3.12)
        - More templates coming soon

        ## Information to Gather:
        - Project name (lowercase, hyphenated, e.g., "my-api")
        - Description (optional)
        - Programming language (C# or Python)
        - Framework (ASP.NET Core Web API or FastAPI)
        - Target platform (GitHub or Azure DevOps)
        - Repository visibility (private, internal, public)
        - Whether to include CI/CD pipeline
        - Whether to include Docker support

        ## Conversation Style:
        - Be concise and helpful
        - Ask clarifying questions when needed
        - Summarize requirements before creating a plan
        - Use markdown formatting for code and lists

        ## Current Status:
        - Platform: {PLATFORM}
        - Organization/Owner: {OWNER}

        When you have gathered enough information, summarize the requirements and ask if the developer wants to proceed with scaffolding.

        Remember: You are in the INTAKE phase. Focus on understanding what the developer needs.
        """;

    public ChatAgent(
        ILogger<ChatAgent> logger,
        IClaudeClient claudeClient,
        ICrispAgent crispAgent,
        IOptions<CrispConfiguration> config)
    {
        _logger = logger;
        _claudeClient = claudeClient;
        _crispAgent = crispAgent;
        _config = config.Value;
    }

    public async Task<ChatMessage> ProcessMessageAsync(
        CrispSession session,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing message in session {SessionId}", session.SessionId);

        // Add user message to history
        session.AddUserMessage(userMessage);

        // Build system prompt with current context
        var systemPrompt = BuildSystemPrompt(session);

        // Get conversation history
        var history = session.GetConversationHistory();

        // Call Claude
        var response = await _claudeClient.SendMessageAsync(
            systemPrompt,
            history,
            cancellationToken);

        // Add assistant response to history
        var assistantMessage = session.AddAssistantMessage(response);

        // Publish event for SSE subscribers
        await session.PublishEventAsync(new AgentMessageEvent(
            assistantMessage.MessageId,
            assistantMessage.Content,
            assistantMessage.Timestamp));

        return assistantMessage;
    }

    public async Task<ChatMessage> HandleApprovalAsync(
        CrispSession session,
        bool approved,
        string? feedback,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling approval for session {SessionId}: Approved={Approved}",
            session.SessionId, approved);

        if (session.Status != SessionStatus.AwaitingApproval)
        {
            throw new InvalidOperationException(
                $"Session is in {session.Status} state, not awaiting approval");
        }

        if (session.CurrentPlan == null)
        {
            throw new InvalidOperationException("No plan available for approval");
        }

        if (!approved)
        {
            // Add feedback and go back to planning
            var feedbackMessage = $"I'd like to make changes: {feedback}";
            session.AddUserMessage(feedbackMessage);
            session.SetStatus(SessionStatus.Planning);

            // Process the feedback through Claude
            return await ProcessMessageAsync(session, feedbackMessage, cancellationToken);
        }

        // Approved - execute the plan
        session.SetStatus(SessionStatus.Executing);
        session.CurrentPlan.IsApproved = true;

        await session.PublishEventAsync(new StepStartedEvent(
            1, "Starting execution...", DateTime.UtcNow));

        try
        {
            var result = await _crispAgent.ExecutePlanAsync(session.CurrentPlan, cancellationToken);
            session.SetDeliveryResult(result);

            if (result.Success)
            {
                session.SetStatus(SessionStatus.Completed);

                var deliveryCard = new DeliveryCardDto(
                    result.Platform,
                    result.RepositoryUrl,
                    result.DefaultBranch,
                    result.PipelineUrl,
                    result.BuildStatus ?? "N/A",
                    result.VsCodeLink);

                var completionMessage = session.AddAssistantMessage(
                    result.SummaryCard ?? "Repository created successfully!",
                    new MessageMetadata(Phase: "Delivery", DeliveryCard: deliveryCard));

                await session.PublishEventAsync(new DeliveryReadyEvent(
                    deliveryCard, DateTime.UtcNow));

                return completionMessage;
            }
            else
            {
                session.SetStatus(SessionStatus.Failed);
                var errorMessage = session.AddAssistantMessage(
                    $"Execution failed: {result.ErrorMessage}");

                await session.PublishEventAsync(new ErrorEvent(
                    result.ErrorMessage ?? "Unknown error", DateTime.UtcNow));

                return errorMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan execution failed");
            session.SetStatus(SessionStatus.Failed);

            var errorMessage = session.AddAssistantMessage(
                $"I encountered an error while executing the plan: {ex.Message}");

            await session.PublishEventAsync(new ErrorEvent(
                ex.Message, DateTime.UtcNow));

            return errorMessage;
        }
    }

    private string BuildSystemPrompt(CrispSession session)
    {
        var config = session.Configuration ?? _config;

        var platform = config.ScmPlatform.ToString();
        var owner = config.ScmPlatform == Core.Enums.ScmPlatform.GitHub
            ? config.GitHub.Owner
            : config.AzureDevOps.Project ?? "Unknown";

        return SystemPrompt
            .Replace("{PLATFORM}", platform)
            .Replace("{OWNER}", owner);
    }
}
