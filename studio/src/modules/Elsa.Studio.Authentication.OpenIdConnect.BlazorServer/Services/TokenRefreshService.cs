using System.Text.Json;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Handles the core OAuth2 refresh token grant flow.
/// This is shared between session token refresh and backend API token acquisition.
/// </summary>
public class TokenRefreshService(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
    ILogger<TokenRefreshService> logger)
{
    /// <summary>
    /// The name of the HTTP client used for token refresh requests (no auth headers).
    /// </summary>
    internal const string AnonymousHttpClientName = "Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Anonymous";
    
    /// <summary>
    /// Default time before token expiry to attempt refresh.
    /// </summary>
    public static readonly TimeSpan DefaultRefreshSkew = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Performs a token refresh using the OAuth2 refresh_token grant.
    /// </summary>
    /// <param name="refreshToken">The refresh token to use.</param>
    /// <param name="scopes">Optional scopes to request. If null/empty, uses the original scopes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the refresh operation.</returns>
    public async Task<TokenRefreshResult> RefreshTokenAsync(
        string refreshToken, 
        string[]? scopes = null,
        CancellationToken cancellationToken = default)
    {
        var oidcOptions = oidcOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var clientId = oidcOptions.ClientId;
        var clientSecret = oidcOptions.ClientSecret;
        string? tokenEndpoint = null;

        // Discover token endpoint from OIDC metadata
        if (oidcOptions.ConfigurationManager != null)
        {
            var config = await oidcOptions.ConfigurationManager.GetConfigurationAsync(cancellationToken);
            tokenEndpoint = config?.TokenEndpoint;
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(tokenEndpoint))
        {
            logger.LogWarning("Could not resolve OIDC configuration for token refresh");
            return TokenRefreshResult.Failed();
        }

        // Build and send request
        var httpClient = httpClientFactory.CreateClient(AnonymousHttpClientName);

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId,
            ["refresh_token"] = refreshToken,
            ["client_secret"] = clientSecret ?? string.Empty
        };

        if (scopes is { Length: > 0 })
        {
            parameters["scope"] = string.Join(" ", scopes);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        request.Content = new FormUrlEncodedContent(
            parameters.Where(x => !string.IsNullOrWhiteSpace(x.Value)));

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Token refresh failed with status {StatusCode}: {Error}", 
                response.StatusCode, errorContent);
            return TokenRefreshResult.Failed();
        }

        // Parse response
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(payload);

        var newAccessToken = doc.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var newRefreshToken = doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresInSeconds = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 0;

        if (string.IsNullOrWhiteSpace(newAccessToken) || expiresInSeconds <= 0)
        {
            logger.LogWarning("Token refresh response missing access_token or expires_in");
            return TokenRefreshResult.Failed();
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);

        logger.LogDebug("Token refreshed successfully, new expiry: {ExpiresAt}", expiresAt);

        return new()
        {
            Success = true,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt
        };
    }
}

