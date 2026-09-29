using System.Globalization;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Cookie authentication events that automatically refresh access tokens when they're about to expire.
/// This is the standard ASP.NET Core pattern.
/// </summary>
public class AuthCookieEvents : CookieAuthenticationEvents
{
    private readonly ISingleFlightCoordinator _refreshCoordinator;
    private readonly TokenRefreshService _tokenRefreshService;
    private readonly ILogger<AuthCookieEvents> _logger;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public AuthCookieEvents(
        ISingleFlightCoordinator refreshCoordinator,
        TokenRefreshService tokenRefreshService,
        ILogger<AuthCookieEvents> logger)
    {
        _refreshCoordinator = refreshCoordinator;
        _tokenRefreshService = tokenRefreshService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var tokens = context.Properties.GetTokens().ToList();
        var refreshToken = tokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
        var expiresAtString = tokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;

        // No refresh token means refresh is not possible - just continue
        if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(expiresAtString))
        {
            await base.ValidatePrincipal(context);
            return;
        }

        if (!DateTimeOffset.TryParse(expiresAtString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt))
        {
            await base.ValidatePrincipal(context);
            return;
        }

        // Check if token is about to expire (within the skew window)
        if (expiresAt > DateTimeOffset.UtcNow.Add(TokenRefreshService.DefaultRefreshSkew))
        {
            // Token still valid, no refresh needed
            await base.ValidatePrincipal(context);
            return;
        }

        _logger.LogDebug("Access token expires at {ExpiresAt}, attempting refresh", expiresAt);

        // Token is expiring soon, attempt refresh
        var refreshed = await TryRefreshTokensAsync(context, refreshToken, tokens);

        if (!refreshed)
        {
            // Refresh failed - reject the principal to force re-authentication
            _logger.LogDebug("Token refresh failed, rejecting principal to trigger re-authentication");
            context.RejectPrincipal();
        }

        await base.ValidatePrincipal(context);
    }

    private async Task<bool> TryRefreshTokensAsync(
        CookieValidatePrincipalContext context, 
        string refreshToken,
        List<AuthenticationToken> tokens)
    {
        var didRefresh = false;

        await _refreshCoordinator.RunAsync(async ct =>
        {
            // Re-check expiry inside the lock (another request may have refreshed already)
            var currentExpiresAtString = tokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;
            if (DateTimeOffset.TryParse(currentExpiresAtString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var currentExpiresAt) &&
                currentExpiresAt > DateTimeOffset.UtcNow.Add(TokenRefreshService.DefaultRefreshSkew))
            {
                didRefresh = true; // Already refreshed by another request
                return 0;
            }

            // Use the shared token refresh service
            var result = await _tokenRefreshService.RefreshTokenAsync(refreshToken, scopes: null, ct);
            
            if (!result.Success)
                return 0;

            // Update the tokens in the authentication properties
            var newTokens = new List<AuthenticationToken>
            {
                new() { Name = "access_token", Value = result.AccessToken! },
                new() { Name = "expires_at", Value = result.ExpiresAt.ToString("o", CultureInfo.InvariantCulture) },
                // Preserve refresh token (use new one if rotated, otherwise keep existing)
                new()
                {
                    Name = "refresh_token",
                    Value = !string.IsNullOrWhiteSpace(result.RefreshToken) ? result.RefreshToken : refreshToken
                }
            };

            // Preserve id_token if present
            var idToken = tokens.FirstOrDefault(t => t.Name == "id_token");
            if (idToken != null)
                newTokens.Add(idToken);

            context.Properties.StoreTokens(newTokens);
            context.ShouldRenew = true; // Tell the cookie handler to reissue the cookie

            didRefresh = true;

            return 0;
        }, CancellationToken.None);

        return didRefresh;
    }
}

