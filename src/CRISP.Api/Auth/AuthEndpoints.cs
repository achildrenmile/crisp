using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRISP.Api.Auth;

/// <summary>
/// Authentication endpoints.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // Login with API key to get JWT token
        group.MapPost("/token", GetToken)
            .WithName("GetToken")
            .WithDescription("Exchange API key for JWT token")
            .AllowAnonymous();

        // Validate current token
        group.MapGet("/validate", ValidateToken)
            .WithName("ValidateToken")
            .WithDescription("Validate current authentication")
            .RequireAuthorization();

        // Get current user info
        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithDescription("Get current authenticated user info")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetToken(
        [FromBody] TokenRequest request,
        IApiKeyValidator apiKeyValidator,
        IJwtTokenService jwtTokenService)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return Results.BadRequest(new { error = "API key is required" });
        }

        var validation = await apiKeyValidator.ValidateAsync(request.ApiKey);
        if (!validation.IsValid)
        {
            return Results.Unauthorized();
        }

        var token = jwtTokenService.GenerateToken(
            validation.KeyId,
            validation.KeyName,
            validation.Roles);

        return Results.Ok(new TokenResponse(
            token,
            "Bearer",
            3600, // 1 hour
            validation.KeyName,
            validation.Roles.ToList()));
    }

    private static IResult ValidateToken(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new { valid = true });
    }

    private static IResult GetCurrentUser(HttpContext context)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = user.Identity.Name;
        var roles = user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var authMethod = user.FindFirst("auth_method")?.Value ?? "jwt";

        return Results.Ok(new UserInfo(userId ?? "", userName ?? "", roles, authMethod));
    }
}

/// <summary>
/// Request to exchange API key for token.
/// </summary>
public record TokenRequest(string ApiKey);

/// <summary>
/// JWT token response.
/// </summary>
public record TokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string UserName,
    List<string> Roles);

/// <summary>
/// Current user information.
/// </summary>
public record UserInfo(
    string Id,
    string Name,
    List<string> Roles,
    string AuthMethod);
