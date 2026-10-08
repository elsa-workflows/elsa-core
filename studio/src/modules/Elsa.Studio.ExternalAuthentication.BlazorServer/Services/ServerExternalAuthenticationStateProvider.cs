using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Services;

/// <summary>
/// Uses the server-only authentication ticket for Studio state and rotates Elsa broker refresh tokens before use.
/// Refresh credentials are never projected into Blazor or browser JavaScript.
/// </summary>
public sealed class ServerExternalAuthenticationStateProvider(
    IHttpContextAccessor httpContextAccessor,
    IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
    ServerExternalAuthenticationRefreshCoordinator refreshCoordinator,
    ExternalAuthenticationClientOptions options) : AuthenticationStateProvider, IExternalAuthenticationTokenProvider
{
    public const string Scheme = "ElsaStudio.ExternalAuthentication.Cookie";
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity())));

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.User.Identity?.IsAuthenticated != true)
            return null;

        var accessToken = await context.GetTokenAsync(Scheme, "access_token");
        var accessExpiresAt = await context.GetTokenAsync(Scheme, "access_expires_at");
        if (!ShouldRefresh(accessExpiresAt))
            return accessToken;

        var refreshToken = await context.GetTokenAsync(Scheme, "refresh_token");
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        try
        {
            var response = await refreshCoordinator.RunAsync(HashRefreshToken(refreshToken), async () =>
            {
                var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
                return await api.ExchangeAsync(new("refresh_token", options.ClientId, RefreshToken: refreshToken), BasicAuthorization(), cancellationToken);
            });
            await SignInAsync(context, CreatePrincipal(response.AccessToken), response, cancellationToken);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return response.AccessToken;
        }
        catch
        {
            await context.SignOutAsync(Scheme);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            return null;
        }
    }

    public async Task SignInAsync(HttpContext context, ClaimsPrincipal principal, BrokerTokenResponse tokens, CancellationToken cancellationToken = default)
    {
        var properties = CreateAuthenticationProperties(tokens, DateTimeOffset.UtcNow);
        await context.SignInAsync(Scheme, principal, properties);
    }

    /// <summary>Creates a ticket whose lifetime is bounded by the broker refresh/external session, not the access token.</summary>
    public static AuthenticationProperties CreateAuthenticationProperties(BrokerTokenResponse tokens, DateTimeOffset now)
    {
        var accessExpiresAt = now.AddSeconds(Math.Max(1, tokens.ExpiresIn));
        var candidates = new[] { tokens.RefreshExpiresIn, tokens.ExternalSessionExpiresIn }
            .Where(seconds => seconds > 0)
            .Select(seconds => now.AddSeconds(seconds))
            .ToArray();
        var sessionExpiresAt = candidates.Length > 0 ? candidates.Min() : accessExpiresAt;
        var properties = new AuthenticationProperties { IsPersistent = true, ExpiresUtc = sessionExpiresAt };
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = tokens.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = tokens.RefreshToken },
            new AuthenticationToken { Name = "access_expires_at", Value = accessExpiresAt.ToString("O") }
        ]);
        return properties;
    }

    public static ClaimsPrincipal CreatePrincipal(string accessToken)
    {
        var identity = new ClaimsIdentity(Scheme);
        try
        {
            var payload = accessToken.Split('.');
            if (payload.Length != 3)
                throw new InvalidOperationException("The broker access token is malformed.");
            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(payload[1])));
            if (!document.RootElement.TryGetProperty("sub", out var subject) || string.IsNullOrWhiteSpace(subject.GetString()))
                throw new InvalidOperationException("The broker access token has no subject.");
            foreach (var property in document.RootElement.EnumerateObject())
                AddClaims(identity, property.Name, property.Value);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("The broker access token is malformed.", exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("The broker access token is malformed.", exception);
        }

        return new(identity);
    }

    private static void AddClaims(ClaimsIdentity identity, string type, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
                AddClaims(identity, type, item);
            return;
        }

        if (value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            identity.AddClaim(new(type, value.ToString()));
    }

    private static bool ShouldRefresh(string? accessExpiresAt)
    {
        if (!DateTimeOffset.TryParse(accessExpiresAt, out var expiresAt))
            return true;
        return expiresAt <= DateTimeOffset.UtcNow.Add(RefreshSkew);
    }

    private string? BasicAuthorization()
    {
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            return null;
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
    }

    private static string HashRefreshToken(string refreshToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
