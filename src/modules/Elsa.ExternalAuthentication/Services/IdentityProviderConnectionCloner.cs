using System.Text.Json;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

internal static class IdentityProviderConnectionCloner
{
    public static IdentityProviderConnection Clone(IdentityProviderConnection source) => new()
    {
        Id = source.Id,
        TenantId = source.TenantId,
        Key = source.Key,
        AdapterType = source.AdapterType,
        AdapterSettingsVersion = source.AdapterSettingsVersion,
        AdapterSettings = CloneJson(source.AdapterSettings),
        SecretBindings = (source.SecretBindings ?? new Dictionary<string, SecretBinding>())
            .ToDictionary(x => x.Key, x => x.Value with { }, StringComparer.Ordinal),
        DisplayName = source.DisplayName,
        IconId = source.IconId,
        DisplayOrder = source.DisplayOrder,
        IsDefault = source.IsDefault,
        IsEnabled = source.IsEnabled,
        ArchivedAt = source.ArchivedAt,
        UnlinkedPolicy = source.UnlinkedPolicy is { } policy
            ? new PolicySelection(policy.Type, policy.SettingsVersion, CloneJson(policy.Settings))
            : null,
        PermissionGrantSources = (source.PermissionGrantSources ?? [])
            .Select(x => new GrantSourceSelection(x.Type, x.SettingsVersion, CloneJson(x.Settings), x.Order))
            .ToArray(),
        ClaimProjection = CloneProjection(source.ClaimProjection),
        UpstreamLogoutMode = source.UpstreamLogoutMode,
        Revision = source.Revision,
        MaterialRevision = source.MaterialRevision,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt
    };

    public static JsonElement CloneJson(JsonElement value) => value.ValueKind == JsonValueKind.Undefined ? default : value.Clone();

    private static ClaimProjection CloneProjection(ClaimProjection? source)
    {
        source ??= ClaimProjection.Empty;
        return new ClaimProjection(
            new HashSet<string>(source.AllowedClaimTypes ?? new HashSet<string>(), StringComparer.Ordinal),
            new HashSet<string>(source.RedactedClaimTypes ?? new HashSet<string>(), StringComparer.Ordinal),
            source.MaximumClaimCount,
            source.MaximumValueLength,
            source.MaximumTotalBytes);
    }
}
