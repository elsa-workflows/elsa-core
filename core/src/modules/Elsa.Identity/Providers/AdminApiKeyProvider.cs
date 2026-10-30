using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using AspNetCore.Authentication.ApiKey;
using Elsa;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.Providers;

/// <summary>
/// Provides an <see cref="IApiKey"/> with admin privileges for an explicitly configured admin API key.
/// </summary>
public class AdminApiKeyProvider(IOptions<AdminApiKeyOptions> options) : IApiKeyProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminApiKeyProvider"/> class with no accepted API key.
    /// </summary>
    [Obsolete("Use the options-based constructor. The built-in admin API key is disabled unless explicitly configured.")]
    public AdminApiKeyProvider() : this(Microsoft.Extensions.Options.Options.Create(new AdminApiKeyOptions()))
    {
    }

    /// <summary>
    /// The all-zero development admin API key. Do not enable in production.
    /// </summary>
    public static readonly string DevelopmentApiKey = Guid.Empty.ToString();

    /// <summary>
    /// The legacy development admin API key.
    /// </summary>
    [Obsolete("Use DevelopmentApiKey. The built-in admin API key is disabled unless explicitly configured.")]
    public static readonly string DefaultApiKey = DevelopmentApiKey;
    
    /// <inheritdoc />
    public Task<IApiKey?> ProvideAsync(string key)
    {
        var apiKeyOptions = options.Value;
        if (string.IsNullOrWhiteSpace(apiKeyOptions.ApiKey) || !SecretEquals(key, apiKeyOptions.ApiKey))
        {
            return Task.FromResult<IApiKey?>(null);
        }

        var permissions = apiKeyOptions.Permissions.Where(permission => !string.IsNullOrWhiteSpace(permission)).ToList();
        List<Claim> claims;
        if (permissions.Count == 0)
        {
            claims = [new Claim(PermissionNames.ClaimType, PermissionNames.None)];
        }
        else
        {
            claims = permissions.Select(permission => new Claim(PermissionNames.ClaimType, permission)).ToList();
        }

        var apiKey = new ApiKey(key, apiKeyOptions.OwnerName, claims);
        return Task.FromResult<IApiKey>(apiKey)!;
    }

    /// <summary>
    /// Compares UTF-16 code units in constant time so unpaired surrogates stay distinct from U+FFFD.
    /// </summary>
    private static bool SecretEquals(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            MemoryMarshal.AsBytes(provided.AsSpan()),
            MemoryMarshal.AsBytes(expected.AsSpan()));
}
