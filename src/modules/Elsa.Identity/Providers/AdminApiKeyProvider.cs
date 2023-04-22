using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using Elsa.Identity.Models;

namespace Elsa.Identity.Providers;

/// <summary>
/// Provides an <see cref="IApiKey"/> with admin privileges for the default admin API key.  
/// </summary>
public class AdminApiKeyProvider : IApiKeyProvider
{
    /// <summary>
    /// The default admin API key.
    /// </summary>
    public static readonly string DefaultApiKey = Guid.Empty.ToString();
    
    /// <inheritdoc />
    public Task<IApiKey?> ProvideAsync(string key)
    {
        if(key != DefaultApiKey)
            return Task.FromResult<IApiKey?>(null);
        
        var claims = new List<Claim> { new("permissions", "*") };
        var apiKey = new ApiKey(key, "admin", claims);
        return Task.FromResult<IApiKey>(apiKey)!;
    }
}