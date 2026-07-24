using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Produces deterministic, non-secret fingerprints for connection material and source snapshots.
/// </summary>
public sealed class ConnectionRevisionCalculator
{
    public string CalculateMaterialRevision(IdentityProviderConnection connection)
    {
        return CalculateHash(writer => WriteMaterial(writer, connection), "m-");
    }

    public string CalculateSourceVersion(ConnectionScope scope, IEnumerable<IdentityProviderConnection> connections)
    {
        return CalculateHash(writer =>
        {
            writer.WriteStartObject();
            WriteScope(writer, scope);
            writer.WritePropertyName("connections");
            writer.WriteStartArray();
            foreach (var connection in connections.OrderBy(x => NormalizeKey(x.Key), StringComparer.Ordinal).ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("id", connection.Id);
                writer.WriteString("key", NormalizeKey(connection.Key));
                writer.WriteString("displayName", connection.DisplayName);
                writer.WriteString("iconId", connection.IconId);
                writer.WriteNumber("displayOrder", connection.DisplayOrder);
                writer.WriteBoolean("isDefault", connection.IsDefault);
                writer.WriteString("materialRevision", CalculateMaterialRevision(connection));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }, "s-");
    }

    public string CalculateRegistryVersion(IEnumerable<(string SourceName, ConnectionSourceOwnership Ownership, ConnectionSourceSnapshot Snapshot)> snapshots)
    {
        return CalculateHash(writer =>
        {
            writer.WriteStartArray();
            foreach (var (sourceName, ownership, snapshot) in snapshots.OrderBy(x => x.SourceName, StringComparer.Ordinal).ThenBy(x => x.Ownership).ThenBy(x => x.Snapshot.Scope.Kind).ThenBy(x => x.Snapshot.Scope.TenantId, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("source", sourceName);
                writer.WriteString("ownership", ownership.ToString());
                WriteScope(writer, snapshot.Scope);
                writer.WriteString("version", snapshot.Version);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }, "r-");
    }

    public static string CalculateConfigurationConnectionId(ConnectionScope scope, string key)
    {
        var payload = $"{scope.Kind}\n{scope.TenantId}\n{NormalizeKey(key)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return $"configuration-{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public static string NormalizeKey(string? key) => key?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string CalculateHash(Action<Utf8JsonWriter> write, string prefix)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            write(writer);

        var hash = SHA256.HashData(stream.ToArray());
        return prefix + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void WriteMaterial(Utf8JsonWriter writer, IdentityProviderConnection connection)
    {
        writer.WriteStartObject();
        writer.WriteString("tenantId", connection.TenantId);
        writer.WriteString("key", NormalizeKey(connection.Key));
        writer.WriteString("adapterType", connection.AdapterType);
        writer.WriteNumber("adapterSettingsVersion", connection.AdapterSettingsVersion);
        writer.WritePropertyName("adapterSettings");
        WriteJson(writer, connection.AdapterSettings);
        writer.WritePropertyName("secretBindings");
        writer.WriteStartArray();
        foreach (var binding in (connection.SecretBindings ?? new Dictionary<string, SecretBinding>()).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("name", binding.Key);
            writer.WriteString("resolverType", binding.Value.ResolverType);
            writer.WriteString("reference", binding.Value.Reference);
            writer.WriteString("expectedType", binding.Value.ExpectedType);
            writer.WriteString("expectedScope", binding.Value.ExpectedScope);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteBoolean("isEnabled", connection.IsEnabled);
        writer.WriteBoolean("isArchived", connection.ArchivedAt.HasValue);
        writer.WriteString("upstreamLogoutMode", connection.UpstreamLogoutMode.ToString());
        writer.WritePropertyName("unlinkedPolicy");
        WritePolicy(writer, connection.UnlinkedPolicy);
        writer.WritePropertyName("permissionGrantSources");
        writer.WriteStartArray();
        foreach (var grantSource in (connection.PermissionGrantSources ?? []).OrderBy(x => x.Order).ThenBy(x => x.Type, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("type", grantSource.Type);
            writer.WriteNumber("settingsVersion", grantSource.SettingsVersion);
            writer.WriteNumber("order", grantSource.Order);
            writer.WritePropertyName("settings");
            WriteJson(writer, grantSource.Settings);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WritePropertyName("claimProjection");
        WriteClaimProjection(writer, connection.ClaimProjection);
        writer.WriteEndObject();
    }

    private static void WriteScope(Utf8JsonWriter writer, ConnectionScope scope)
    {
        writer.WriteString("scopeKind", scope.Kind.ToString());
        writer.WriteString("scopeTenantId", scope.TenantId);
    }

    private static void WritePolicy(Utf8JsonWriter writer, PolicySelection? policy)
    {
        if (policy is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("type", policy.Type);
        writer.WriteNumber("settingsVersion", policy.SettingsVersion);
        writer.WritePropertyName("settings");
        WriteJson(writer, policy.Settings);
        writer.WriteEndObject();
    }

    private static void WriteClaimProjection(Utf8JsonWriter writer, ClaimProjection? projection)
    {
        projection ??= ClaimProjection.Empty;
        writer.WriteStartObject();
        WriteStringSet(writer, "allowedClaimTypes", projection.AllowedClaimTypes);
        WriteStringSet(writer, "redactedClaimTypes", projection.RedactedClaimTypes);
        writer.WriteNumber("maximumClaimCount", projection.MaximumClaimCount);
        writer.WriteNumber("maximumValueLength", projection.MaximumValueLength);
        writer.WriteNumber("maximumTotalBytes", projection.MaximumTotalBytes);
        writer.WriteEndObject();
    }

    private static void WriteStringSet(Utf8JsonWriter writer, string propertyName, IReadOnlySet<string>? values)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();
        foreach (var value in values is null ? Enumerable.Empty<string>() : values.OrderBy(x => x, StringComparer.Ordinal))
            writer.WriteStringValue(value);
        writer.WriteEndArray();
    }

    private static void WriteJson(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Undefined:
                writer.WriteNullValue();
                break;
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteJson(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray())
                    WriteJson(writer, item);
                writer.WriteEndArray();
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }
}
