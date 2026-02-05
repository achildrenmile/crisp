using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CRISP.Api.Auth;

/// <summary>
/// Authentication handler for API key validation.
/// </summary>
public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
{
    private readonly IApiKeyValidator _apiKeyValidator;

    public ApiKeyAuthHandler(
        IOptionsMonitor<ApiKeyAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyValidator apiKeyValidator)
        : base(options, logger, encoder)
    {
        _apiKeyValidator = apiKeyValidator;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for API key in header
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.Fail("API key is empty");
        }

        var validationResult = await _apiKeyValidator.ValidateAsync(apiKey);
        if (!validationResult.IsValid)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, validationResult.KeyName),
            new(ClaimTypes.NameIdentifier, validationResult.KeyId),
            new("auth_method", "api_key")
        };

        foreach (var role in validationResult.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}

/// <summary>
/// Options for API key authentication.
/// </summary>
public class ApiKeyAuthOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string HeaderName { get; set; } = "X-API-Key";
}

/// <summary>
/// Result of API key validation.
/// </summary>
public record ApiKeyValidationResult(
    bool IsValid,
    string KeyId = "",
    string KeyName = "",
    IReadOnlyList<string>? Roles = null)
{
    public IReadOnlyList<string> Roles { get; } = Roles ?? Array.Empty<string>();

    public static ApiKeyValidationResult Invalid() => new(false);
    public static ApiKeyValidationResult Valid(string keyId, string keyName, IReadOnlyList<string> roles)
        => new(true, keyId, keyName, roles);
}

/// <summary>
/// Interface for API key validation.
/// </summary>
public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey);
}
