using System.Collections.Concurrent;
using System.Security.Claims;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Blazor Server implementation of <see cref="ITokenProvider"/> that retrieves tokens from the authenticated HTTP context.
/// </summary>
public class ServerTokenProvider(
    IHttpContextAccessor httpContextAccessor,
    ISingleFlightCoordinator refreshCoordinator,
    OidcOptions oidcOptions,
    TokenRefreshService tokenRefreshService)
    : ITokenProvider
{
    // Simple in-memory cache for backend API tokens (when different scopes are needed)
    private static readonly ConcurrentDictionary<string, CachedToken> TokenCache = new();

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        var backendScopes = oidcOptions.BackendApiScopes?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray() ?? [];
        
        // If no specific backend scopes configured, just use the cookie's access token
        if (backendScopes.Length == 0)
        {
            return await httpContext.GetTokenAsync("access_token");
        }

        // Need a token with specific scopes - check cache or acquire new one
        return await GetScopedAccessTokenAsync(httpContext, backendScopes, cancellationToken);
    }
    
    private async Task<string?> GetScopedAccessTokenAsync(HttpContext httpContext, string[] scopes, CancellationToken cancellationToken)
    {
        var userKey = httpContext.User.FindFirstValue("sub") 
                      ?? httpContext.User.FindFirstValue("oid") 
                      ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrWhiteSpace(userKey))
            return null;

        var cacheKey = $"{userKey}:{string.Join(",", scopes.OrderBy(s => s))}";

        // Check cache
        if (TokenCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return cached.AccessToken;
        }

        // Get refresh token to acquire new backend token
        var refreshToken = await httpContext.GetTokenAsync("refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        string? newToken = null;

        await refreshCoordinator.RunAsync(async ct =>
        {
            // Re-check cache after lock
            if (TokenCache.TryGetValue(cacheKey, out var cachedAfterLock) && cachedAfterLock.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                newToken = cachedAfterLock.AccessToken;
                return 0;
            }

            var result = await tokenRefreshService.RefreshTokenAsync(refreshToken, scopes, ct);

            if (!result.Success)
                return 0;

            TokenCache[cacheKey] = new CachedToken(result.AccessToken!, result.ExpiresAt);
            newToken = result.AccessToken;
            return 0;
        }, cancellationToken);

        return newToken;
    }

    private record CachedToken(string AccessToken, DateTimeOffset ExpiresAt);
}
