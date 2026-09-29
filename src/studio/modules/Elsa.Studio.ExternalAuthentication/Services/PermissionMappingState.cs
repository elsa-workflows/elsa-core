using System.Text.Json;
using Elsa.Studio.ExternalAuthentication.Models;

namespace Elsa.Studio.ExternalAuthentication.Services;

public static class PermissionMappingState
{
    public static PermissionGrantSourceSelection Create(PermissionGrantSourceDescriptor descriptor, int order) => new()
    {
        Type = descriptor.Type,
        SettingsVersion = descriptor.SettingsVersion,
        Settings = descriptor.Fields
            .Where(field => field.DefaultValue.HasValue)
            .ToDictionary(field => field.Name, field => field.DefaultValue!.Value.Clone(), StringComparer.Ordinal),
        Order = order
    };

    public static IReadOnlyCollection<string> GetReferencedPermissions(IEnumerable<PermissionGrantSourceSelection> sources)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources)
            VisitObject(source.Settings, permissions);
        return permissions.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyCollection<string> ValidateDelegation(
        IEnumerable<PermissionGrantSourceSelection> sources,
        IReadOnlySet<string> actorPermissions,
        bool canDelegateUnrestricted)
    {
        if (canDelegateUnrestricted || actorPermissions.Contains("*"))
            return [];

        return GetReferencedPermissions(sources)
            .Where(permission => !actorPermissions.Contains(permission))
            .Select(permission => $"You cannot delegate '{permission}' because it is not granted to your current Elsa identity.")
            .ToArray();
    }

    public static IReadOnlyCollection<string> GetDescriptorWarnings(
        IEnumerable<PermissionGrantSourceSelection> sources,
        IEnumerable<PermissionDescriptor> descriptors)
    {
        var known = descriptors.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        return GetReferencedPermissions(sources)
            .Where(permission => !known.Contains(permission))
            .Select(permission => $"'{permission}' is not advertised by an installed module. It remains valid, but only an endpoint requiring that exact string will recognize it.")
            .ToArray();
    }

    public static string SerializeSettings(PermissionGrantSourceSelection source) => JsonSerializer.Serialize(source.Settings);

    public static bool TrySetSettings(PermissionGrantSourceSelection source, string json, out string? error)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Grant source settings must be a JSON object.";
                return false;
            }

            source.Settings = document.RootElement.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
            error = null;
            return true;
        }
        catch (JsonException)
        {
            error = "Grant source settings must be valid JSON.";
            return false;
        }
    }

    private static void VisitObject(IReadOnlyDictionary<string, JsonElement> settings, ISet<string> permissions)
    {
        foreach (var setting in settings)
        {
            if (setting.Key.Equals("permissions", StringComparison.OrdinalIgnoreCase) ||
                setting.Key.Equals("allowedPermissions", StringComparison.OrdinalIgnoreCase))
            {
                AddStrings(setting.Value, permissions);
            }

            VisitElement(setting.Value, permissions);
        }
    }

    private static void VisitElement(JsonElement element, ISet<string> permissions)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals("permissions", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("allowedPermissions", StringComparison.OrdinalIgnoreCase))
                    AddStrings(property.Value, permissions);
                VisitElement(property.Value, permissions);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                VisitElement(item, permissions);
        }
    }

    private static void AddStrings(JsonElement element, ISet<string> permissions)
    {
        if (element.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(element.GetString()))
            permissions.Add(element.GetString()!);
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var value in element.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String))
            {
                if (!string.IsNullOrWhiteSpace(value.GetString()))
                    permissions.Add(value.GetString()!);
            }
        }
    }
}
