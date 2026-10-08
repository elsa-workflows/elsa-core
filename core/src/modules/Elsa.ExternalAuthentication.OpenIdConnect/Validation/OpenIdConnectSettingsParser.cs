using System.Text.Json;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.OpenIdConnect.Models;

namespace Elsa.ExternalAuthentication.OpenIdConnect.Validation;

public sealed class OpenIdConnectSettingsParser
{
    public bool TryParse(JsonElement settings, out OpenIdConnectConnectionSettings? result, out IReadOnlyCollection<ConnectionValidationError> errors)
    {
        var validationErrors = new List<ConnectionValidationError>();
        result = null;

        if (settings.ValueKind != JsonValueKind.Object)
        {
            validationErrors.Add(new("settings", "invalid", "OpenID Connect settings must be a JSON object."));
            errors = validationErrors;
            return false;
        }

        var mode = GetString(settings, "mode") ?? "discovery";
        var trustMode = mode.Equals("manual", StringComparison.OrdinalIgnoreCase) ? OpenIdConnectTrustMode.Manual : OpenIdConnectTrustMode.Discovery;
        if (!mode.Equals("discovery", StringComparison.OrdinalIgnoreCase) && trustMode != OpenIdConnectTrustMode.Manual)
            validationErrors.Add(new("mode", "invalid", "Mode must be discovery or manual."));

        var discoveryUrl = GetHttpsUri(settings, "discoveryUrl", validationErrors, trustMode == OpenIdConnectTrustMode.Discovery);
        var clientId = GetString(settings, "clientId");
        if (string.IsNullOrWhiteSpace(clientId))
            validationErrors.Add(new("clientId", "required", "Client ID is required."));

        var clientAuthentication = GetString(settings, "clientAuthenticationMethod") ?? "client_secret_basic";
        var clientAuthenticationMethod = clientAuthentication.Equals("client_secret_post", StringComparison.OrdinalIgnoreCase)
            ? OpenIdConnectClientAuthenticationMethod.ClientSecretPost
            : OpenIdConnectClientAuthenticationMethod.ClientSecretBasic;
        if (!clientAuthentication.Equals("client_secret_basic", StringComparison.OrdinalIgnoreCase) && clientAuthenticationMethod != OpenIdConnectClientAuthenticationMethod.ClientSecretPost)
            validationErrors.Add(new("clientAuthenticationMethod", "invalid", "Client authentication must be client_secret_basic or client_secret_post."));
        if (string.Equals(GetString(settings, "providerPkce"), "disabled", StringComparison.OrdinalIgnoreCase))
            validationErrors.Add(new("providerPkce", "invalid", "Upstream PKCE is always required."));

        var issuer = GetString(settings, "issuer");
        var authorizationEndpoint = GetHttpsUri(settings, "authorizationEndpoint", validationErrors, trustMode == OpenIdConnectTrustMode.Manual);
        var tokenEndpoint = GetHttpsUri(settings, "tokenEndpoint", validationErrors, trustMode == OpenIdConnectTrustMode.Manual);
        var userInfoEndpoint = GetHttpsUri(settings, "userInfoEndpoint", validationErrors, false);
        var endSessionEndpoint = GetHttpsUri(settings, "endSessionEndpoint", validationErrors, false);
        var jwksUri = GetHttpsUri(settings, "jwksUri", validationErrors, false);
        var signingKeys = settings.TryGetProperty("signingKeys", out var signingKeysElement) ? signingKeysElement.Clone() : default;

        if (trustMode == OpenIdConnectTrustMode.Manual)
        {
            if (string.IsNullOrWhiteSpace(issuer))
                validationErrors.Add(new("issuer", "required", "Manual trust requires an issuer."));

            if (signingKeys.ValueKind is not JsonValueKind.Object && jwksUri is null)
                validationErrors.Add(new("signingKeys", "required", "Manual trust requires signing keys or a JWKS URI."));
        }

        var scopes = GetStringArray(settings, "scopes");
        if (!scopes.Contains("openid", StringComparer.Ordinal))
            scopes = ["openid", .. scopes];

        errors = validationErrors;
        if (validationErrors.Count != 0 || string.IsNullOrWhiteSpace(clientId))
            return false;

        result = new(
            trustMode,
            discoveryUrl,
            clientId,
            clientAuthenticationMethod,
            scopes,
            OpenIdConnectProviderPkceMode.Required,
            issuer,
            authorizationEndpoint,
            tokenEndpoint,
            userInfoEndpoint,
            endSessionEndpoint,
            jwksUri,
            signingKeys);
        return true;
    }

    private static string? GetString(JsonElement settings, string propertyName) => settings.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Uri? GetHttpsUri(JsonElement settings, string propertyName, ICollection<ConnectionValidationError> errors, bool required)
    {
        var value = GetString(settings, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
                errors.Add(new(propertyName, "required", $"{propertyName} is required."));
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
        {
            errors.Add(new(propertyName, "invalid", $"{propertyName} must be an absolute HTTPS URI without user info or a fragment."));
            return null;
        }

        return uri;
    }

    private static IReadOnlyCollection<string> GetStringArray(JsonElement settings, string propertyName)
    {
        if (!settings.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
            return [];

        return values.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct(StringComparer.Ordinal).ToArray();
    }
}
