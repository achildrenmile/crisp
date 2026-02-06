namespace CRISP.Api.Auth;

/// <summary>
/// Configuration for authentication and authorization.
/// </summary>
public class AuthConfiguration
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Whether authentication is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// JWT configuration.
    /// </summary>
    public JwtConfiguration Jwt { get; set; } = new();

    /// <summary>
    /// OIDC configuration for external identity providers.
    /// </summary>
    public OidcConfiguration? Oidc { get; set; }

    /// <summary>
    /// Configured API keys.
    /// </summary>
    public List<ApiKeyConfig> ApiKeys { get; set; } = [];
}

/// <summary>
/// OpenID Connect configuration for external identity providers.
/// </summary>
public class OidcConfiguration
{
    /// <summary>
    /// Whether OIDC authentication is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The OIDC authority URL (e.g., https://login.microsoftonline.com/{tenant-id}/v2.0).
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The client ID registered with the identity provider.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret for confidential client authentication.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The response type for the OIDC flow (default: code).
    /// </summary>
    public string ResponseType { get; set; } = "code";

    /// <summary>
    /// Scopes to request from the identity provider.
    /// </summary>
    public List<string> Scopes { get; set; } = ["openid", "profile", "email"];

    /// <summary>
    /// The callback path for OIDC authentication (default: /signin-oidc).
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// The signed out callback path (default: /signout-callback-oidc).
    /// </summary>
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

    /// <summary>
    /// Whether to save tokens in the authentication properties.
    /// </summary>
    public bool SaveTokens { get; set; } = true;

    /// <summary>
    /// Whether to get claims from the user info endpoint.
    /// </summary>
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;

    /// <summary>
    /// Cookie configuration for OIDC sessions.
    /// </summary>
    public OidcCookieConfiguration Cookie { get; set; } = new();
}

/// <summary>
/// Cookie configuration for OIDC authentication.
/// </summary>
public class OidcCookieConfiguration
{
    /// <summary>
    /// Cookie name (default: .CRISP.Auth).
    /// </summary>
    public string Name { get; set; } = ".CRISP.Auth";

    /// <summary>
    /// SameSite mode for the cookie (default: Lax).
    /// </summary>
    public string SameSite { get; set; } = "Lax";

    /// <summary>
    /// Whether the cookie requires HTTPS (default: true in production).
    /// </summary>
    public bool SecurePolicy { get; set; } = true;

    /// <summary>
    /// Cookie expiration in minutes (default: 60).
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// JWT token configuration.
/// </summary>
public class JwtConfiguration
{
    /// <summary>
    /// Secret key for signing tokens (min 32 characters).
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer.
    /// </summary>
    public string Issuer { get; set; } = "CRISP";

    /// <summary>
    /// Token audience.
    /// </summary>
    public string Audience { get; set; } = "CRISP-Web";

    /// <summary>
    /// Token expiration in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}

/// <summary>
/// Configuration for a single API key.
/// </summary>
public class ApiKeyConfig
{
    /// <summary>
    /// Unique identifier for this key.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name for this key.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The API key value (or hash if IsHashed is true).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Whether the key is stored as a SHA256 hash.
    /// </summary>
    public bool IsHashed { get; set; }

    /// <summary>
    /// Roles assigned to this API key.
    /// </summary>
    public List<string> Roles { get; set; } = ["User"];
}
