using System.Text.Json.Nodes;

namespace Elsa.ReactStudio.Host;

/// <summary>Resolves the browser-visible configuration document without rebuilding frontend assets.</summary>
internal static class StudioConfiguration
{
    internal static async Task<JsonObject> LoadAsync(string contentRoot, string defaultFile,
        Func<string, string?> environment, CancellationToken cancellationToken)
    {
        var inline = environment("STUDIO_CONFIG");
        var file = System.IO.Path.GetFullPath(environment("STUDIO_CONFIG_PATH") ?? defaultFile, contentRoot);
        var text = !string.IsNullOrEmpty(inline) ? inline : File.Exists(file)
            ? await File.ReadAllTextAsync(file, cancellationToken) : "{}";
        var document = JsonNode.Parse(text) as JsonObject ?? throw new InvalidDataException("Expected a configuration object");
        Set(document, "title", environment("STUDIO_TITLE"));

        var serverUrl = environment("STUDIO_SERVER_URL");
        var serverName = environment("STUDIO_SERVER_NAME");
        var tenantId = environment("STUDIO_TENANT_ID");
        if (!string.IsNullOrEmpty(serverUrl) || !string.IsNullOrEmpty(serverName) || tenantId is not null)
        {
            var servers = document["servers"] as JsonArray ?? new JsonArray();
            var first = servers.FirstOrDefault() as JsonObject;
            first = first?.DeepClone().AsObject() ?? new JsonObject();
            Set(first, "url", serverUrl);
            Set(first, "name", serverName);
            first["name"] ??= "Default";
            if (tenantId is not null)
            {
                first["tenantId"] = tenantId;
            }
            if (servers.Count == 0)
            {
                servers.Add(first);
            }
            else
            {
                servers[0] = first;
            }
            document["servers"] = servers.DeepClone();
        }

        var auth = document["auth"] as JsonObject ?? new JsonObject();
        Set(auth, "provider", environment("STUDIO_AUTH_PROVIDER"));
        if (environment("STUDIO_API_KEY") is { Length: > 0 } key)
        {
            var apiKey = auth["apiKey"] as JsonObject ?? new JsonObject();
            apiKey["key"] = key;
            auth["apiKey"] = apiKey.DeepClone();
        }
        var oidc = auth["oidc"] as JsonObject ?? new JsonObject();
        Set(oidc, "authority", environment("STUDIO_OIDC_AUTHORITY"));
        Set(oidc, "clientId", environment("STUDIO_OIDC_CLIENT_ID"));
        Set(oidc, "scope", environment("STUDIO_OIDC_SCOPE"));
        auth["oidc"] = oidc.DeepClone();
        document["auth"] = auth.DeepClone();

        if (environment("STUDIO_FEATURES") is { Length: > 0 } enabled)
        {
            var features = document["features"] as JsonObject ?? new JsonObject();
            foreach (var id in enabled.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var feature = features[id] as JsonObject ?? new JsonObject();
                feature["enabled"] = true;
                features[id] = feature.DeepClone();
            }
            document["features"] = features.DeepClone();
        }

        if (document["servers"] is not JsonArray { Count: > 0 } configuredServers ||
            configuredServers.Any(server => string.IsNullOrWhiteSpace(server?["url"]?.GetValue<string>())))
        {
            throw new InvalidDataException("At least one server URL is required");
        }
        return document;
    }

    private static void Set(JsonObject parent, string property, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            parent[property] = value;
        }
    }
}
