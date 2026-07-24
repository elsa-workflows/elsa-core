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
            validationErrors.Add(new ConnectionValidationError("settings", "invalid", "OpenID Connect settings must be a JSON object."));
            errors = validationErrors;
            return false;
        }

        var mode = GetString(settings, "mode") ?? "discovery";
        var trustMode = mode.Equals("manual", StringComparison.OrdinalIgnoreCase) ? OpenIdConnectTrustMode.Manual : OpenIdConnectTrustMode.Discovery;
        if (!mode.Equals("discovery", StringComparison.OrdinalIgnoreCase) && trustMode != OpenIdConnectTrustMode.Manual)
            validationErrors.Add(new ConnectionValidationError("mode", "invalid", "Mode must be discovery or manual."));

        var authority = GetHttpsUri(settings, "authority", validationErrors, true);
        var clientId = GetString(settings, "clientId");
        if (string.IsNullOrWhiteSpace(clientId))
            validationErrors.Add(new ConnectionValidationError("clientId", "required", "Client ID is required."));

        var callbackUri = GetHttpsUri(settings, "callbackUri", validationErrors, true);
        var providerPkce = GetString(settings, "providerPkce") ?? "required";
        var pkceMode = providerPkce.Equals("disabled", StringComparison.OrdinalIgnoreCase) ? OpenIdConnectProviderPkceMode.Disabled : OpenIdConnectProviderPkceMode.Required;
        if (!providerPkce.Equals("required", StringComparison.OrdinalIgnoreCase) && pkceMode != OpenIdConnectProviderPkceMode.Disabled)
            validationErrors.Add(new ConnectionValidationError("providerPkce", "invalid", "Provider PKCE must be required or disabled."));

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
                validationErrors.Add(new ConnectionValidationError("issuer", "required", "Manual trust requires an issuer."));

            if (signingKeys.ValueKind is not JsonValueKind.Object && jwksUri is null)
                validationErrors.Add(new ConnectionValidationError("signingKeys", "required", "Manual trust requires signing keys or a JWKS URI."));
        }

        var scopes = GetStringArray(settings, "scopes");
        if (!scopes.Contains("openid", StringComparer.Ordinal))
            scopes = ["openid", .. scopes];

        errors = validationErrors;
        if (validationErrors.Count != 0 || authority is null || callbackUri is null || string.IsNullOrWhiteSpace(clientId))
            return false;

        result = new OpenIdConnectConnectionSettings(
            trustMode,
            authority,
            clientId,
            callbackUri,
            scopes,
            pkceMode,
            issuer,
            authorizationEndpoint,
            tokenEndpoint,
            userInfoEndpoint,
            endSessionEndpoint,
            jwksUri,
            signingKeys,
            settings.TryGetProperty("useUserInfo", out var useUserInfo) && useUserInfo.ValueKind == JsonValueKind.True);
        return true;
    }

    private static string? GetString(JsonElement settings, string propertyName) => settings.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Uri? GetHttpsUri(JsonElement settings, string propertyName, ICollection<ConnectionValidationError> errors, bool required)
    {
        var value = GetString(settings, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
                errors.Add(new ConnectionValidationError(propertyName, "required", $"{propertyName} is required."));
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
        {
            errors.Add(new ConnectionValidationError(propertyName, "invalid", $"{propertyName} must be an absolute HTTPS URI without user info or a fragment."));
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
