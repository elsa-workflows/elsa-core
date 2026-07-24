using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.OpenIdConnect.Models;
using Elsa.ExternalAuthentication.OpenIdConnect.Validation;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.ExternalAuthentication.OpenIdConnect.Services;

/// <summary>Implements hardened OpenID Connect authorization-code authentication for the protocol-neutral Elsa broker.</summary>
public sealed class OpenIdConnectExternalAuthenticationAdapter(IProviderHttpClient providerHttpClient, OpenIdConnectSettingsParser settingsParser) : IExternalAuthenticationAdapter
{
    public const string AdapterType = "openid-connect";
    public string Type => AdapterType;

    public ExternalAuthenticationAdapterDescriptor Describe() => new(
        AdapterType,
        "OpenID Connect",
        "Authenticates users with an OpenID Connect authorization-code provider.",
        1,
        [
            new("authority", "Authority", "Base provider authority.", "uri", true, "uri", null, [], new(1, 2048), false, false, null, null, false),
            new("clientId", "Client ID", "Provider client registration identifier.", "string", true, "text", null, [], new(1, 512), false, false, null, null, false),
            new("clientSecret", "Client secret", "Provider client secret binding.", "secret", false, "secret", null, [], new(), true, false, null, null, true),
            new("callbackUri", "Callback URI", "Fixed Elsa provider callback URI.", "uri", true, "uri", null, [], new(), false, false, null, null, false),
            new("mode", "Trust mode", "Discovery is the default. Manual trust requires explicit endpoints and signing keys.", "string", true, "select", null, ["discovery", "manual"], new(), false, false, null, null, false),
            new("scopes", "Scopes", "OpenID Connect scopes requested from the provider. The openid scope is always included.", "string-array", false, "tags", null, [], new(), false, false, null, null, false),
            new("issuer", "Issuer", "Expected issuer for manually configured provider trust.", "uri", true, "uri", null, [], new(), false, false, new("mode", "manual"), null, false),
            new("authorizationEndpoint", "Authorization endpoint", "Provider authorization endpoint for manual trust.", "uri", true, "uri", null, [], new(), false, false, new("mode", "manual"), null, false),
            new("tokenEndpoint", "Token endpoint", "Provider token endpoint for manual trust.", "uri", true, "uri", null, [], new(), false, false, new("mode", "manual"), null, false),
            new("jwksUri", "JWKS URI", "Provider signing-key endpoint for manual trust.", "uri", false, "uri", null, [], new(), false, false, new("mode", "manual"), null, false),
            new("signingKeys", "Signing keys", "Pinned JSON Web Key Set for manual trust.", "json", false, "json", null, [], new(), false, true, new("mode", "manual"), null, true),
            new("userInfoEndpoint", "UserInfo endpoint", "Optional provider UserInfo endpoint.", "uri", false, "uri", null, [], new(), false, false, null, null, false),
            new("useUserInfo", "Use UserInfo", "Fetch optional UserInfo after ID-token validation.", "boolean", false, "switch", null, [], new(), false, false, null, null, false),
            new("endSessionEndpoint", "End-session endpoint", "Optional upstream logout endpoint.", "uri", false, "uri", null, [], new(), false, false, null, null, false),
            new("providerPkce", "Provider PKCE", "Use S256 PKCE for the upstream authorization-code flow.", "string", true, "select", null, ["required", "disabled"], new(), false, true, null, null, false)
        ],
        new(true, true, true),
        null);

    public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default)
    {
        var parsed = settingsParser.TryParse(context.Connection.Connection.AdapterSettings, out _, out var errors);
        return ValueTask.FromResult(new ConnectionValidationResult(parsed, errors, []));
    }

    public async ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(context.Connection.Connection.AdapterSettings, cancellationToken);
        var metadata = await ResolveMetadataAsync(settings, cancellationToken);
        var nonce = context.Transaction.ProviderNonce ?? CreateRandomValue();
        var verifier = settings.ProviderPkce == OpenIdConnectProviderPkceMode.Required ? CreateRandomValue() : null;
        var state = JsonSerializer.SerializeToUtf8Bytes(new OpenIdConnectAdapterState(metadata.Issuer, nonce, verifier));
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = settings.CallbackUri.AbsoluteUri,
            ["scope"] = string.Join(' ', settings.Scopes),
            ["state"] = context.CorrelationState,
            ["nonce"] = nonce
        };

        if (verifier is not null)
        {
            query["code_challenge"] = CreateCodeChallenge(verifier);
            query["code_challenge_method"] = "S256";
        }

        return new ExternalAuthorizationRequest(WithQuery(metadata.AuthorizationEndpoint, query), state);
    }

    public async ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default)
    {
        if (TryGetParameter(context.Parameters, "error", out _))
            throw new OpenIdConnectAuthenticationException("The identity provider rejected the authentication request.");

        if (!TryGetParameter(context.Parameters, "state", out var state) || !FixedTimeEquals(state, context.CorrelationState))
            throw new OpenIdConnectAuthenticationException("The identity provider callback could not be correlated.");

        var settings = await GetSettingsAsync(context.Connection.Connection.AdapterSettings, cancellationToken);
        var metadata = await ResolveMetadataAsync(settings, cancellationToken);
        var adapterState = ReadAdapterState(context.Transaction.ProtectedPayload);
        if (adapterState is not null && !string.Equals(adapterState.Issuer, metadata.Issuer, StringComparison.Ordinal))
            throw new OpenIdConnectAuthenticationException("The identity provider callback issuer did not match the initiated request.");

        var idToken = await ExchangeCodeAsync(settings, metadata, context, adapterState?.CodeVerifier, cancellationToken);
        var principal = await ValidateIdTokenAsync(idToken, settings, metadata, cancellationToken);
        var nonce = principal.FindFirst("nonce")?.Value;
        var expectedNonce = context.Transaction.ProviderNonce ?? adapterState?.Nonce;
        if (string.IsNullOrWhiteSpace(expectedNonce) || !FixedTimeEquals(nonce, expectedNonce))
            throw new OpenIdConnectAuthenticationException("The identity provider nonce did not match the initiated request.");

        var issuer = principal.FindFirst("iss")?.Value ?? metadata.Issuer;
        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
            throw new OpenIdConnectAuthenticationException("The identity provider response did not contain a subject.");

        var projectedClaims = ProjectClaims(principal, context.Connection.Connection.ClaimProjection);
        return new ExternalAuthenticationResult(new ExternalIdentity(issuer, subject, projectedClaims), projectedClaims, []);
    }

    public async ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(context.Connection.Connection.AdapterSettings, cancellationToken);
        _ = await ResolveMetadataAsync(settings, cancellationToken);
        return new ConnectionTestResult(ConnectionObservationStatus.Succeeded, "reachable", "Provider metadata was resolved.", []);
    }

    public async ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(context.Connection.Connection.AdapterSettings, cancellationToken);
        var metadata = await ResolveMetadataAsync(settings, cancellationToken);
        return metadata.EndSessionEndpoint is null
            ? null
            : new ExternalLogoutRequest(WithQuery(metadata.EndSessionEndpoint, new Dictionary<string, string> { ["post_logout_redirect_uri"] = context.Transaction.CallbackUri.AbsoluteUri, ["state"] = context.CorrelationState }), []);
    }

    private async Task<OpenIdConnectConnectionSettings> GetSettingsAsync(JsonElement settings, CancellationToken cancellationToken)
    {
        if (!settingsParser.TryParse(settings, out var parsed, out _))
            throw new OpenIdConnectAuthenticationException("The OpenID Connect connection configuration is invalid.");
        await Task.CompletedTask;
        return parsed!;
    }

    private async Task<ProviderMetadata> ResolveMetadataAsync(OpenIdConnectConnectionSettings settings, CancellationToken cancellationToken)
    {
        if (settings.TrustMode == OpenIdConnectTrustMode.Manual)
            return new ProviderMetadata(settings.Issuer!, settings.AuthorizationEndpoint!, settings.TokenEndpoint!, settings.UserInfoEndpoint, settings.EndSessionEndpoint, settings.JwksUri, settings.SigningKeys);

        var address = new Uri(settings.Authority.AbsoluteUri.TrimEnd('/') + "/.well-known/openid-configuration");
        var response = await providerHttpClient.GetAsync(address, ProviderResponseKind.Discovery, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new OpenIdConnectAuthenticationException("The identity provider metadata could not be resolved.");
        using var document = ParseProviderJson(response.Body, "The identity provider metadata could not be resolved.");
        var root = document.RootElement;
        var issuer = GetRequiredHttpsUri(root, "issuer").AbsoluteUri.TrimEnd('/');
        var authorizationEndpoint = GetRequiredHttpsUri(root, "authorization_endpoint");
        var tokenEndpoint = GetRequiredHttpsUri(root, "token_endpoint");
        return new ProviderMetadata(issuer, authorizationEndpoint, tokenEndpoint, GetOptionalHttpsUri(root, "userinfo_endpoint"), GetOptionalHttpsUri(root, "end_session_endpoint"), GetOptionalHttpsUri(root, "jwks_uri"), default);
    }

    private async Task<string> ExchangeCodeAsync(OpenIdConnectConnectionSettings settings, ProviderMetadata metadata, ExternalCallbackContext context, string? verifier, CancellationToken cancellationToken)
    {
        if (!TryGetParameter(context.Parameters, "code", out var code))
            throw new OpenIdConnectAuthenticationException("The identity provider callback did not contain an authorization code.");

        var values = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = settings.CallbackUri.AbsoluteUri,
            ["client_id"] = settings.ClientId
        };
        if (verifier is not null)
            values["code_verifier"] = verifier;
        if (context.Secrets.TryGetValue("clientSecret", out var secret))
            values["client_secret"] = secret.Value.Reveal();

        var response = await providerHttpClient.PostFormAsync(metadata.TokenEndpoint, values, ProviderResponseKind.Token, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new OpenIdConnectAuthenticationException("The identity provider token exchange failed.");
        using var payload = ParseProviderJson(response.Body, "The identity provider token response was invalid.");
        if (!payload.RootElement.TryGetProperty("id_token", out var idToken) || idToken.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(idToken.GetString()))
            throw new OpenIdConnectAuthenticationException("The identity provider token response did not contain an ID token.");
        return idToken.GetString()!;
    }

    private async Task<System.Security.Claims.ClaimsPrincipal> ValidateIdTokenAsync(string idToken, OpenIdConnectConnectionSettings settings, ProviderMetadata metadata, CancellationToken cancellationToken)
    {
        var signingKeys = metadata.SigningKeys.ValueKind == JsonValueKind.Object
            ? new JsonWebKeySet(metadata.SigningKeys.GetRawText()).Keys
            : await GetSigningKeysAsync(metadata.JwksUri, cancellationToken);
        var validation = await new JsonWebTokenHandler { MapInboundClaims = false }.ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = metadata.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.ClientId,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            RequireSignedTokens = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        });
        if (!validation.IsValid || validation.ClaimsIdentity is null)
            throw new OpenIdConnectAuthenticationException("The identity provider ID token was invalid.");

        var audiences = validation.ClaimsIdentity.FindAll("aud").Select(x => x.Value).Distinct(StringComparer.Ordinal).ToArray();
        if (audiences.Length > 1 && !string.Equals(validation.ClaimsIdentity.FindFirst("azp")?.Value, settings.ClientId, StringComparison.Ordinal))
            throw new OpenIdConnectAuthenticationException("The identity provider ID token was not authorized for this client.");
        return new System.Security.Claims.ClaimsPrincipal(validation.ClaimsIdentity);
    }

    private async Task<IEnumerable<SecurityKey>> GetSigningKeysAsync(Uri? jwksUri, CancellationToken cancellationToken)
    {
        if (jwksUri is null)
            throw new OpenIdConnectAuthenticationException("The identity provider did not provide signing keys.");
        var response = await providerHttpClient.GetAsync(jwksUri, ProviderResponseKind.SigningKeys, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new OpenIdConnectAuthenticationException("The identity provider signing keys could not be resolved.");
        try
        {
            return new JsonWebKeySet(response.ReadBodyAsUtf8()).Keys;
        }
        catch (JsonException)
        {
            throw new OpenIdConnectAuthenticationException("The identity provider signing keys were invalid.");
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> ProjectClaims(System.Security.Claims.ClaimsPrincipal principal, ClaimProjection projection)
    {
        if (projection.MaximumClaimCount <= 0 || projection.MaximumValueLength <= 0 || projection.MaximumTotalBytes <= 0)
            return new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal);

        var allowed = projection.AllowedClaimTypes ?? new HashSet<string>();
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var count = 0;
        var bytes = 0;

        foreach (var claim in principal.Claims)
        {
            if (!allowed.Contains(claim.Type) || claim.Value.Length > projection.MaximumValueLength || count == projection.MaximumClaimCount)
                continue;

            var valueBytes = Encoding.UTF8.GetByteCount(claim.Value);
            if (valueBytes > projection.MaximumTotalBytes - bytes)
                continue;

            if (!result.TryGetValue(claim.Type, out var values))
                result[claim.Type] = values = [];
            values.Add(claim.Value);
            count++;
            bytes += valueBytes;
        }

        return result.ToDictionary(x => x.Key, x => (IReadOnlyCollection<string>)x.Value.ToArray(), StringComparer.Ordinal);
    }

    private static OpenIdConnectAdapterState? ReadAdapterState(byte[] payload) => payload.Length == 0 ? null : JsonSerializer.Deserialize<OpenIdConnectAdapterState>(payload);

    private static JsonDocument ParseProviderJson(byte[] payload, string safeMessage)
    {
        try
        {
            return JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            throw new OpenIdConnectAuthenticationException(safeMessage);
        }
    }
    private static bool TryGetParameter(IReadOnlyDictionary<string, IReadOnlyCollection<string>> values, string key, out string value) { value = values.TryGetValue(key, out var found) ? found.FirstOrDefault() ?? string.Empty : string.Empty; return !string.IsNullOrEmpty(value); }
    private static string CreateRandomValue() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    private static string CreateCodeChallenge(string verifier) => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    private static bool FixedTimeEquals(string? left, string? right) => left is not null && right is not null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
    private static Uri WithQuery(Uri uri, IReadOnlyDictionary<string, string> values) => new(uri.AbsoluteUri + (uri.Query.Length == 0 ? "?" : "&") + string.Join("&", values.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}")));
    private static Uri GetRequiredHttpsUri(JsonElement value, string property)
    {
        if (!value.TryGetProperty(property, out var item) || item.ValueKind != JsonValueKind.String || !TryGetHttpsUri(item.GetString(), out var uri))
            throw new OpenIdConnectAuthenticationException("The identity provider metadata was incomplete or unsafe.");
        return uri;
    }

    private static Uri? GetOptionalHttpsUri(JsonElement value, string property)
    {
        if (!value.TryGetProperty(property, out var item))
            return null;
        if (item.ValueKind != JsonValueKind.String || !TryGetHttpsUri(item.GetString(), out var uri))
            throw new OpenIdConnectAuthenticationException("The identity provider metadata contained an unsafe endpoint.");
        return uri;
    }

    private static bool TryGetHttpsUri(string? value, out Uri uri) => Uri.TryCreate(value, UriKind.Absolute, out uri!) && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment);

    private sealed record ProviderMetadata(string Issuer, Uri AuthorizationEndpoint, Uri TokenEndpoint, Uri? UserInfoEndpoint, Uri? EndSessionEndpoint, Uri? JwksUri, JsonElement SigningKeys);
}
