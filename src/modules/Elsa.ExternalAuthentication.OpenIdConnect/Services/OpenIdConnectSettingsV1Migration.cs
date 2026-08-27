using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.ExternalAuthentication.Contracts;

namespace Elsa.ExternalAuthentication.OpenIdConnect.Services;

/// <summary>Migrates the unreleased v1 authority/callback settings to the v2 deployment-derived callback model.</summary>
public sealed class OpenIdConnectSettingsV1Migration : IAdapterSettingsMigration
{
    public string AdapterType => OpenIdConnectExternalAuthenticationAdapter.AdapterType;
    public int FromVersion => 1;
    public int ToVersion => 2;

    public ValueTask<JsonElement> MigrateAsync(JsonElement settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var node = JsonNode.Parse(settings.GetRawText())?.AsObject() ?? throw new InvalidOperationException("OpenID Connect settings must be an object.");
        if (!node.ContainsKey("discoveryUrl") && node["authority"]?.GetValue<string>() is { Length: > 0 } authority)
            node["discoveryUrl"] = authority.TrimEnd('/') + "/.well-known/openid-configuration";
        node.Remove("authority");
        node.Remove("callbackUri");
        node["providerPkce"] = "required";
        node["clientAuthenticationMethod"] ??= "client_secret_basic";
        using var document = JsonDocument.Parse(node.ToJsonString());
        return ValueTask.FromResult(document.RootElement.Clone());
    }
}
