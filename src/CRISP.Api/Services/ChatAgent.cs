using System.Text.Json;
using CRISP.Api.Models;
using CRISP.Core.Configuration;
using CRISP.Core.Enums;
using CRISP.Core.Interfaces;
using CRISP.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRISP.Api.Services;

/// <summary>
/// Chat-based agent implementation that uses Claude for conversation
/// and executes actual scaffolding operations.
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
        3. When you have enough information, output a JSON block to create the project

        ## Project Types You Support:
        - ASP.NET Core Web API (.NET 8) - use language: "CSharp", framework: "AspNetCoreWebApi"
        - FastAPI (Python 3.12) - use language: "Python", framework: "FastApi"

        ## Required Information:
        - Project name (lowercase, hyphenated, e.g., "my-api")
        - Programming language (CSharp or Python)
        - Framework (AspNetCoreWebApi or FastApi)

        ## Optional Information (with defaults):
        - Description (default: empty)
        - Repository visibility: Private, Internal, or Public (default: Private)
        - Include Docker support (container): true/false (default: true)

        ## When Ready to Create:
        When you have gathered enough information, output EXACTLY this JSON block (no other text):

        ```json
        {
            "action": "create_project",
            "requirements": {
                "projectName": "the-project-name",
                "description": "Optional description",
                "language": "CSharp",
                "framework": "AspNetCoreWebApi",
                "visibility": "Private",
                "includeDocker": true
            }
        }
        ```

        ## Conversation Style:
        - Be concise and helpful
        - Ask clarifying questions when needed
        - Confirm the project name and type before creating
        - If user says things like "yes", "go ahead", "create it", "do it" after you've summarized requirements, output the JSON block

        ## Current Configuration:
        - Platform: {PLATFORM}
        - Organization/Owner: {OWNER}

        IMPORTANT: Only output the JSON block when you're ready to create the project. Otherwise, have a normal conversation.
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

        _logger.LogInformation("Claude response: {Response}", response);

        // Check if response contains a create_project action
        var actionResult = TryParseAction(response);

        if (actionResult != null && actionResult.Action == "create_project")
        {
            _logger.LogInformation("Detected create_project action, executing scaffolding...");

            // Execute the actual scaffolding
            return await ExecuteScaffoldingAsync(session, actionResult.Requirements!, cancellationToken);
        }

        // Regular conversation - add assistant response to history
        var assistantMessage = session.AddAssistantMessage(response);

        // Publish event for SSE subscribers
        await session.PublishEventAsync(new AgentMessageEvent(
            assistantMessage.MessageId,
            assistantMessage.Content,
            assistantMessage.Timestamp));

        return assistantMessage;
    }

    private async Task<ChatMessage> ExecuteScaffoldingAsync(
        CrispSession session,
        ProjectRequirementsDto requirementsDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Convert DTO to domain model
            // Determine runtime version based on language
            var runtimeVersion = requirementsDto.Language.Equals("CSharp", StringComparison.OrdinalIgnoreCase)
                ? ".NET 8"
                : "Python 3.12";

            var requirements = new ProjectRequirements
            {
                ProjectName = requirementsDto.ProjectName,
                Description = requirementsDto.Description ?? $"Repository created by CRISP for {requirementsDto.ProjectName}",
                Language = Enum.Parse<ProjectLanguage>(requirementsDto.Language, ignoreCase: true),
                RuntimeVersion = runtimeVersion,
                Framework = Enum.Parse<ProjectFramework>(requirementsDto.Framework, ignoreCase: true),
                ScmPlatform = _config.ScmPlatform,
                Visibility = Enum.TryParse<RepositoryVisibility>(requirementsDto.Visibility, ignoreCase: true, out var vis)
                    ? vis
                    : RepositoryVisibility.Private,
                IncludeContainerSupport = requirementsDto.IncludeDocker,
                TestingFramework = requirementsDto.Language.Equals("CSharp", StringComparison.OrdinalIgnoreCase)
                    ? "xUnit"
                    : "pytest"
            };

            // Send progress message
            var startMessage = $"""
                🚀 **Starting Project Scaffolding**

                Creating **{requirements.ProjectName}** with:
                - Language: {requirements.Language}
                - Framework: {requirements.Framework}
                - Platform: {_config.ScmPlatform}
                - Visibility: {requirements.Visibility}
                - Docker: {(requirements.IncludeContainerSupport ? "Yes" : "No")}

                Please wait while I create your repository...
                """;

            session.AddAssistantMessage(startMessage);
            await session.PublishEventAsync(new AgentMessageEvent(
                Guid.NewGuid().ToString(),
                startMessage,
                DateTime.UtcNow));

            // Create the plan
            _logger.LogInformation("Creating execution plan for {ProjectName}", requirements.ProjectName);
            var plan = await _crispAgent.CreatePlanAsync(requirements, cancellationToken);

            // Auto-approve for now (in production, would wait for user approval)
            plan.IsApproved = true;
            session.SetPlan(plan);
            session.SetStatus(SessionStatus.Executing);

            // Execute the plan
            _logger.LogInformation("Executing plan for {ProjectName}", requirements.ProjectName);
            var result = await _crispAgent.ExecutePlanAsync(plan, cancellationToken);

            session.SetDeliveryResult(result);

            if (result.Success)
            {
                session.SetStatus(SessionStatus.Completed);

                var successMessage = $"""
                    ✅ **Repository Created Successfully!**

                    📦 **Repository Details:**
                    - **URL:** [{result.RepositoryUrl}]({result.RepositoryUrl})
                    - **Branch:** {result.DefaultBranch}
                    - **Platform:** {result.Platform}
                    {(result.PipelineUrl != null ? $"- **CI/CD:** [{result.PipelineUrl}]({result.PipelineUrl})" : "")}
                    {(result.BuildStatus != null && result.BuildStatus != "N/A" ? $"- **Build Status:** {result.BuildStatus}" : "")}

                    🚀 **Next Steps:**
                    1. Clone the repository:
                       ```bash
                       git clone {result.CloneUrl}
                       ```
                    2. Open in VS Code: [Click here]({result.VsCodeLink})
                    3. Follow the README for setup instructions

                    Happy coding! 🎉
                    """;

                var assistantMessage = session.AddAssistantMessage(successMessage);

                var deliveryCard = new DeliveryCardDto(
                    result.Platform,
                    result.RepositoryUrl,
                    result.DefaultBranch,
                    result.PipelineUrl,
                    result.BuildStatus ?? "N/A",
                    result.VsCodeLink);

                await session.PublishEventAsync(new DeliveryReadyEvent(
                    deliveryCard, DateTime.UtcNow));

                return assistantMessage;
            }
            else
            {
                session.SetStatus(SessionStatus.Failed);

                var errorMessage = $"""
                    ❌ **Scaffolding Failed**

                    I encountered an error while creating your repository:

                    ```
                    {result.ErrorMessage}
                    ```

                    Please check the configuration and try again. Common issues:
                    - Invalid GitHub token or permissions
                    - Repository name already exists
                    - Network connectivity issues

                    Would you like me to try again with different settings?
                    """;

                var assistantMessage = session.AddAssistantMessage(errorMessage);

                await session.PublishEventAsync(new ErrorEvent(
                    result.ErrorMessage ?? "Unknown error", DateTime.UtcNow));

                return assistantMessage;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scaffolding failed with exception");
            session.SetStatus(SessionStatus.Failed);

            var errorMessage = $"""
                ❌ **Scaffolding Failed**

                An unexpected error occurred:

                ```
                {ex.Message}
                ```

                Please try again or contact support if the issue persists.
                """;

            var assistantMessage = session.AddAssistantMessage(errorMessage);

            await session.PublishEventAsync(new ErrorEvent(
                ex.Message, DateTime.UtcNow));

            return assistantMessage;
        }
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
        var owner = config.ScmPlatform == ScmPlatform.GitHub
            ? config.GitHub.Owner
            : config.AzureDevOps.Project ?? "Unknown";

        return SystemPrompt
            .Replace("{PLATFORM}", platform)
            .Replace("{OWNER}", owner);
    }

    private ActionResult? TryParseAction(string response)
    {
        try
        {
            // Look for JSON block in the response
            var jsonStart = response.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
            var jsonEnd = response.LastIndexOf("```", StringComparison.OrdinalIgnoreCase);

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart + 7, jsonEnd - jsonStart - 7).Trim();
                _logger.LogInformation("Found JSON block: {Json}", jsonContent);

                var result = JsonSerializer.Deserialize<ActionResult>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }

            // Also try to parse the entire response as JSON (in case Claude outputs raw JSON)
            if (response.TrimStart().StartsWith('{'))
            {
                var result = JsonSerializer.Deserialize<ActionResult>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }

            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse action from response");
            return null;
        }
    }

    private sealed class ActionResult
    {
        public string Action { get; set; } = string.Empty;
        public ProjectRequirementsDto? Requirements { get; set; }
    }

    private sealed class ProjectRequirementsDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Language { get; set; } = "CSharp";
        public string Framework { get; set; } = "AspNetCoreWebApi";
        public string Visibility { get; set; } = "Private";
        public bool IncludeDocker { get; set; } = true;
    }
}
