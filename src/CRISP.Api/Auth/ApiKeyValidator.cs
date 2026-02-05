using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace CRISP.Api.Auth;

/// <summary>
/// Validates API keys against configured keys.
/// </summary>
public class ApiKeyValidator : IApiKeyValidator
{
    private readonly ILogger<ApiKeyValidator> _logger;
    private readonly AuthConfiguration _config;

    public ApiKeyValidator(
        ILogger<ApiKeyValidator> logger,
        IOptions<AuthConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(ApiKeyValidationResult.Invalid());
        }

        // Check against configured API keys
        foreach (var configuredKey in _config.ApiKeys)
        {
            if (ValidateKey(apiKey, configuredKey))
            {
                _logger.LogInformation("API key validated: {KeyName}", configuredKey.Name);
                return Task.FromResult(ApiKeyValidationResult.Valid(
                    configuredKey.Id,
                    configuredKey.Name,
                    configuredKey.Roles));
            }
        }

        _logger.LogWarning("Invalid API key attempted");
        return Task.FromResult(ApiKeyValidationResult.Invalid());
    }

    private static bool ValidateKey(string providedKey, ApiKeyConfig configuredKey)
    {
        // If key is stored as hash, compare hashes
        if (configuredKey.IsHashed)
        {
            var providedHash = HashKey(providedKey);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedHash),
                Encoding.UTF8.GetBytes(configuredKey.Key));
        }

        // Plain text comparison (for development only)
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey),
            Encoding.UTF8.GetBytes(configuredKey.Key));
    }

    public static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(bytes);
    }

    public static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"crisp_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }
}
