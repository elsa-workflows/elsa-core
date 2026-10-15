using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Platform.Integration.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Platform.Integration.Services;

public sealed class FileShellConfigurationOverlayStore(
    IOptions<ElsaPlatformIntegrationOptions> options,
    IHostEnvironment environment) : IShellConfigurationOverlayStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<bool> ConfigureFeaturesAsync(
        string shellId,
        IReadOnlyDictionary<string, JsonElement>? enabledFeatures,
        IReadOnlyList<string>? disabledFeatures,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var root = await LoadAsync(cancellationToken);
            var before = Serialize(root);
            var features = EnsureObject(root, "CShells", "Shells", shellId, "Features");

            foreach (var feature in enabledFeatures ?? new Dictionary<string, JsonElement>())
                features[feature.Key] = ToNode(feature.Value) ?? new JsonObject();

            foreach (var featureId in disabledFeatures ?? [])
                features.Remove(featureId);

            var state = EnsureObject(root, "Elsa", "PlatformIntegration", "Shells", shellId);
            var disabledFeatureArray = new JsonArray();
            foreach (var featureId in disabledFeatures ?? [])
                disabledFeatureArray.Add(featureId);
            state["DisabledFeatures"] = disabledFeatureArray;

            return await SaveIfChangedAsync(root, before, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ConfigureSettingsAsync(
        string shellId,
        JsonElement settings,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var root = await LoadAsync(cancellationToken);
            var before = Serialize(root);
            var configuration = EnsureObject(root, "CShells", "Shells", shellId, "Configuration");
            if (ToNode(settings) is JsonObject settingsObject)
                Merge(configuration, settingsObject);

            return await SaveIfChangedAsync(root, before, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<JsonObject> LoadAsync(CancellationToken cancellationToken)
    {
        var path = GetPath();
        if (!File.Exists(path))
            return new JsonObject();

        await using var stream = File.OpenRead(path);
        return await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken) as JsonObject
            ?? throw new InvalidOperationException("The Platform shell overlay file must contain a JSON object.");
    }

    private async Task<bool> SaveIfChangedAsync(JsonObject root, string before, CancellationToken cancellationToken)
    {
        var after = Serialize(root);
        if (string.Equals(before, after, StringComparison.Ordinal))
            return false;

        var path = GetPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(tempPath, after, cancellationToken);
        File.Move(tempPath, path, overwrite: true);
        return true;
    }

    private string GetPath()
    {
        var path = options.Value.ShellOverlayPath;
        return Path.IsPathRooted(path) ? path : Path.Combine(environment.ContentRootPath, path);
    }

    private static JsonObject EnsureObject(JsonObject root, params string[] path)
    {
        JsonObject current = root;
        foreach (var segment in path)
        {
            if (current[segment] is not JsonObject child)
            {
                child = new JsonObject();
                current[segment] = child;
            }

            current = child;
        }

        return current;
    }

    private static JsonNode? ToNode(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Undefined or JsonValueKind.Null => null,
            _ => JsonNode.Parse(element.GetRawText())
        };

    private static void Merge(JsonObject target, JsonObject source)
    {
        foreach (var property in source)
        {
            if (property.Value is JsonObject sourceObject && target[property.Key] is JsonObject targetObject)
            {
                Merge(targetObject, sourceObject);
                continue;
            }

            target[property.Key] = property.Value?.DeepClone();
        }
    }

    private static string Serialize(JsonObject root) =>
        root.ToJsonString(JsonOptions);
}
