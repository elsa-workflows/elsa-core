using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Providers;

/// <summary>
/// Supplies deployment-owned connections from the current External Authentication options.
/// </summary>
public sealed class ConfigurationIdentityProviderConnectionSource(
    IOptionsMonitor<ExternalAuthenticationOptions> options,
    ConnectionRevisionCalculator revisionCalculator,
    IExternalAuthenticationAdapterRegistry adapters) : IIdentityProviderConnectionSource
{
    public const string SourceName = "configuration";

    public string Name => SourceName;
    public ConnectionSourceOwnership Ownership => ConnectionSourceOwnership.Configuration;

    public ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(ConnectionScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var connections = (options.CurrentValue.ConfigurationConnections ?? [])
            .Where(connection => IsInScope(connection.TenantId, scope))
            .Select(connection => Materialize(connection, scope))
            .OrderBy(connection => ConnectionRevisionCalculator.NormalizeKey(connection.Key), StringComparer.Ordinal)
            .ThenBy(connection => connection.Id, StringComparer.Ordinal)
            .ToArray();

        return ValueTask.FromResult(new ConnectionSourceSnapshot(scope, revisionCalculator.CalculateSourceVersion(scope, connections), connections));
    }

    private IdentityProviderConnection Materialize(IdentityProviderConnection configuredConnection, ConnectionScope scope)
    {
        if (adapters.TryGet(configuredConnection.AdapterType, out var adapter))
            AdapterSettingsSecretFieldGuard.ThrowIfContainsDeclaredSecret(configuredConnection.AdapterSettings, adapter.Describe(), configuredConnection.Key);

        var connection = new IdentityProviderConnection
        {
            Id = string.IsNullOrWhiteSpace(configuredConnection.Id)
                ? ConnectionRevisionCalculator.CalculateConfigurationConnectionId(scope, configuredConnection.Key)
                : configuredConnection.Id,
            TenantId = scope.TenantId,
            Key = ConnectionRevisionCalculator.NormalizeKey(configuredConnection.Key),
            AdapterType = configuredConnection.AdapterType,
            AdapterSettingsVersion = configuredConnection.AdapterSettingsVersion,
            AdapterSettings = CloneJson(configuredConnection.AdapterSettings),
            SecretBindings = configuredConnection.SecretBindings?.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal) ?? new Dictionary<string, SecretBinding>(StringComparer.Ordinal),
            DisplayName = configuredConnection.DisplayName,
            IconId = configuredConnection.IconId,
            DisplayOrder = configuredConnection.DisplayOrder,
            IsPreferred = configuredConnection.IsPreferred,
            IsEnabled = configuredConnection.IsEnabled,
            OverridesConfigurationConnection = false,
            ArchivedAt = configuredConnection.ArchivedAt,
            UnlinkedPolicy = configuredConnection.UnlinkedPolicy is { } policy ? new PolicySelection(policy.Type, policy.SettingsVersion, CloneJson(policy.Settings)) : null,
            PermissionGrantSources = (configuredConnection.PermissionGrantSources ?? [])
                .Select(x => new GrantSourceSelection(x.Type, x.SettingsVersion, CloneJson(x.Settings), x.Order))
                .ToArray(),
            ClaimProjection = CloneClaimProjection(configuredConnection.ClaimProjection),
            UpstreamLogoutMode = configuredConnection.UpstreamLogoutMode,
            Revision = 1,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };

        connection.MaterialRevision = revisionCalculator.CalculateMaterialRevision(connection);
        return connection;
    }

    private static bool IsInScope(string? tenantId, ConnectionScope scope) =>
        scope == ConnectionScope.Host && (string.IsNullOrWhiteSpace(tenantId) || string.Equals(tenantId, ConnectionScope.HostTenantId, StringComparison.Ordinal));

    private static ClaimProjection CloneClaimProjection(ClaimProjection? projection)
    {
        projection ??= ClaimProjection.Empty;
        return new(
            new HashSet<string>(projection.AllowedClaimTypes ?? new HashSet<string>(), StringComparer.Ordinal),
            new HashSet<string>(projection.RedactedClaimTypes ?? new HashSet<string>(), StringComparer.Ordinal),
            projection.MaximumClaimCount,
            projection.MaximumValueLength,
            projection.MaximumTotalBytes);
    }

    private static JsonElement CloneJson(JsonElement value) => value.ValueKind == JsonValueKind.Undefined ? default : value.Clone();
}
