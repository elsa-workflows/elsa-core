using System.Text.Json;

namespace Elsa.ExternalAuthentication.Permissions;

internal static class PermissionGrantMappingSettings
{
    public static IReadOnlyCollection<PermissionGrantMapping> Read(JsonElement settings)
    {
        if (settings.ValueKind != JsonValueKind.Object || !settings.TryGetProperty("claimType", out var claimTypeProperty) || claimTypeProperty.ValueKind != JsonValueKind.String)
            return [];

        var claimType = claimTypeProperty.GetString();
        if (string.IsNullOrWhiteSpace(claimType) || !settings.TryGetProperty("mappings", out var mappings))
            return [];

        var result = new List<PermissionGrantMapping>();
        if (mappings.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in mappings.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                result.Add(new PermissionGrantMapping(claimType, property.Name, ReadPermissions(property.Value)));
        }
        else if (mappings.ValueKind == JsonValueKind.Array)
        {
            foreach (var mapping in mappings.EnumerateArray())
            {
                if (mapping.ValueKind != JsonValueKind.Object || !mapping.TryGetProperty("value", out var valueProperty) || valueProperty.ValueKind != JsonValueKind.String || !mapping.TryGetProperty("permissions", out var permissionsProperty))
                    continue;

                var value = valueProperty.GetString();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                result.Add(new PermissionGrantMapping(claimType, value, ReadPermissions(permissionsProperty)));
            }
        }

        return result;
    }

    public static IReadOnlyCollection<string> ReadPassThroughPermissions(JsonElement settings)
    {
        if (settings.ValueKind != JsonValueKind.Object || !settings.TryGetProperty("allowedPermissions", out var permissions))
            return [];

        return ReadPermissions(permissions);
    }

    private static IReadOnlyCollection<string> ReadPermissions(JsonElement value) => value.ValueKind == JsonValueKind.Array
        ? value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray()
        : [];
}

internal sealed record PermissionGrantMapping(string ClaimType, string Value, IReadOnlyCollection<string> Permissions);
