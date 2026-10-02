using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Projects the broker-issued Elsa JWT into Blazor authentication state without persisting it by default.</summary>
public sealed class ExternalAuthenticationWasmAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly ExternalAuthenticationWasmTokenProvider tokenProvider;

    /// <summary>Creates the browser authentication state projection.</summary>
    public ExternalAuthenticationWasmAuthenticationStateProvider(ExternalAuthenticationWasmTokenProvider tokenProvider)
    {
        this.tokenProvider = tokenProvider;
        tokenProvider.TokensChanged += NotifyTokenChange;
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenProvider.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return new(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = ReadClaims(token).ToArray();
        if (claims.Length == 0)
            return new(new ClaimsPrincipal(new ClaimsIdentity()));

        return new(new ClaimsPrincipal(new ClaimsIdentity(claims, "broker")));
    }

    /// <inheritdoc />
    public void Dispose() => tokenProvider.TokensChanged -= NotifyTokenChange;

    private void NotifyTokenChange() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private static IEnumerable<Claim> ReadClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
            return [];

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return document.RootElement.EnumerateObject().SelectMany(ToClaims).ToArray();
        }
        catch (FormatException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<Claim> ToClaims(JsonProperty property)
    {
        if (property.Value.ValueKind == JsonValueKind.Array)
            return property.Value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => new Claim(property.Name, x.GetString()!));

        return property.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False
            ? [new Claim(property.Name, property.Value.ToString())]
            : [];
    }
}
