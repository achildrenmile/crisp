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
    /// Configured API keys.
    /// </summary>
    public List<ApiKeyConfig> ApiKeys { get; set; } = [];
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
