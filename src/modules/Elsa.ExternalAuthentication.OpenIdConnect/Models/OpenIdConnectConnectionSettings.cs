using System.Text.Json;

namespace Elsa.ExternalAuthentication.OpenIdConnect.Models;

/// <summary>Selects discovery-based or explicitly pinned OpenID Connect trust metadata.</summary>
public enum OpenIdConnectTrustMode
{
    Discovery,
    Manual
}

/// <summary>Controls S256 PKCE on the upstream provider authorization-code flow.</summary>
public enum OpenIdConnectProviderPkceMode
{
    Required,
    Disabled
}

/// <summary>Validated version-1 settings owned by the OpenID Connect adapter.</summary>
public sealed record OpenIdConnectConnectionSettings(
    OpenIdConnectTrustMode TrustMode,
    Uri Authority,
    string ClientId,
    Uri CallbackUri,
    IReadOnlyCollection<string> Scopes,
    OpenIdConnectProviderPkceMode ProviderPkce,
    string? Issuer,
    Uri? AuthorizationEndpoint,
    Uri? TokenEndpoint,
    Uri? UserInfoEndpoint,
    Uri? EndSessionEndpoint,
    Uri? JwksUri,
    JsonElement SigningKeys,
    bool UseUserInfo);

/// <summary>Protected provider state persisted only for one correlated authorization attempt.</summary>
public sealed record OpenIdConnectAdapterState(string Issuer, string Nonce, string? CodeVerifier);

/// <summary>A safe OpenID Connect processing failure suitable for conversion to a public broker category.</summary>
public sealed class OpenIdConnectAuthenticationException(string safeMessage) : InvalidOperationException(safeMessage);
