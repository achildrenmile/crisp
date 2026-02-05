using System.Text.Json;
using CRISP.Api.Models;
using CRISP.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CRISP.Api.Endpoints;

/// <summary>
/// Chat API endpoints for CRISP sessions.
/// </summary>
public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat")
            .WithTags("Chat");

        // Create a new session
        group.MapPost("/sessions", CreateSession)
            .WithName("CreateSession")
            .WithDescription("Creates a new CRISP chat session");

        // Send a message
        group.MapPost("/sessions/{sessionId}/messages", SendMessage)
            .WithName("SendMessage")
            .WithDescription("Sends a message to the agent");

        // Get message history
        group.MapGet("/sessions/{sessionId}/messages", GetMessages)
            .WithName("GetMessages")
            .WithDescription("Gets all messages in the session");

        // Get SSE event stream
        group.MapGet("/sessions/{sessionId}/events", GetEvents)
            .WithName("GetEvents")
            .WithDescription("Server-Sent Events stream for real-time updates");

        // Approve or reject plan
        group.MapPost("/sessions/{sessionId}/approve", ApprovePlan)
            .WithName("ApprovePlan")
            .WithDescription("Approves or rejects the execution plan");

        // Get session status
        group.MapGet("/sessions/{sessionId}/status", GetStatus)
            .WithName("GetStatus")
            .WithDescription("Gets the current session status");

        // Get delivery result
        group.MapGet("/sessions/{sessionId}/result", GetResult)
            .WithName("GetResult")
            .WithDescription("Gets the delivery result (when completed)");
    }

    private static IResult CreateSession(
        [FromBody] CreateSessionRequest? request,
        ISessionManager sessionManager)
    {
        var session = sessionManager.CreateSession(request?.Configuration);

        return Results.Created(
            $"/api/chat/sessions/{session.SessionId}",
            new CreateSessionResponse(
                session.SessionId,
                session.Status.ToString(),
                session.CreatedAt));
    }

    private static async Task<IResult> SendMessage(
        string sessionId,
        [FromBody] SendMessageRequest request,
        ISessionManager sessionManager,
        IChatAgent chatAgent,
        CancellationToken cancellationToken)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session == null)
        {
            return Results.NotFound(new { error = "Session not found" });
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(new { error = "Message content is required" });
        }

        try
        {
            var response = await chatAgent.ProcessMessageAsync(
                session, request.Content, cancellationToken);

            return Results.Ok(new SendMessageResponse(response.MessageId, response.Role, response.Content, response.Timestamp));
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to process message");
        }
    }

    private static IResult GetMessages(
        string sessionId,
        ISessionManager sessionManager)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session == null)
        {
            return Results.NotFound(new { error = "Session not found" });
        }

        return Results.Ok(session.Messages);
    }

    private static async Task GetEvents(
        string sessionId,
        HttpContext context,
        ISessionManager sessionManager,
        CancellationToken cancellationToken)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session == null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        try
        {
            await foreach (var evt in session.EventReader.ReadAllAsync(cancellationToken))
            {
                var eventData = JsonSerializer.Serialize(evt, jsonOptions);
                await context.Response.WriteAsync($"event: {evt.Type}\n", cancellationToken);
                await context.Response.WriteAsync($"data: {eventData}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is normal
        }
    }

    private static async Task<IResult> ApprovePlan(
        string sessionId,
        [FromBody] ApproveRequest request,
        ISessionManager sessionManager,
        IChatAgent chatAgent,
        CancellationToken cancellationToken)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session == null)
        {
            return Results.NotFound(new { error = "Session not found" });
        }

        try
        {
            var response = await chatAgent.HandleApprovalAsync(
                session, request.Approved, request.Feedback, cancellationToken);

            return Results.Ok(new SendMessageResponse(response.MessageId, response.Role, response.Content, response.Timestamp));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: 500,
                title: "Failed to process approval");
        }
    }

    private static IResult GetStatus(
        string sessionId,
        ISessionManager sessionManager)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session == null)
        {
            return Results.NotFound(new { error = "Session not found" });
        }

        return Results.Ok(new SessionStatusResponse(
            session.SessionId,
            session.Status,
            session.Messages.Count,
            session.CreatedAt,
            session.LastActivityAt));
    }

    private static IResult GetResult(
        string sessionId,
        ISessionManager sessionManager)
    {
        var session = sessionManager.GetSession(sessionId);
        if (session == null)
        {
            return Results.NotFound(new { error = "Session not found" });
        }

        if (session.Status != SessionStatus.Completed)
        {
            return Results.BadRequest(new
            {
                error = "Session is not completed",
                currentStatus = session.Status.ToString()
            });
        }

        if (session.DeliveryResult == null)
        {
            return Results.BadRequest(new { error = "No delivery result available" });
        }

        var result = session.DeliveryResult;
        return Results.Ok(new DeliveryCardDto(
            result.Platform,
            result.RepositoryUrl,
            result.DefaultBranch,
            result.PipelineUrl,
            result.BuildStatus ?? "N/A",
            result.VsCodeLink));
    }
}
