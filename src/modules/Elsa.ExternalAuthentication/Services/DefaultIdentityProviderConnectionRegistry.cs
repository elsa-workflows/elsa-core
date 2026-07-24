using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Builds the read-through effective connection registry from configuration and optional persisted sources.
/// </summary>
public sealed class DefaultIdentityProviderConnectionRegistry(
    IEnumerable<IIdentityProviderConnectionSource> sources,
    ConnectionRevisionCalculator revisionCalculator) : IIdentityProviderConnectionRegistry
{
    private readonly IReadOnlyList<IIdentityProviderConnectionSource> _sources = sources
        .OrderBy(x => GetOwnershipPriority(x.Ownership))
        .ThenBy(x => x.Name, StringComparer.Ordinal)
        .ToArray();

    public async ValueTask<EffectiveConnectionRegistry> GetAsync(string targetTenantId, CancellationToken cancellationToken = default)
    {
        var scopes = GetApplicableScopes(targetTenantId);
        var snapshots = new List<(IIdentityProviderConnectionSource Source, ConnectionSourceSnapshot Snapshot)>();

        foreach (var scope in scopes)
        foreach (var source in _sources)
        {
            var snapshot = await source.GetSnapshotAsync(scope, cancellationToken);
            if (snapshot.Scope != scope)
                throw new InvalidOperationException($"Connection source '{source.Name}' returned a snapshot for a scope it was not asked to resolve.");

            snapshots.Add((source, snapshot));
        }

        var candidates = snapshots
            .SelectMany(x => x.Snapshot.Connections.Select(connection => new Candidate(x.Source, x.Snapshot.Scope, connection)))
            .Where(x => IsInScope(x.Connection, x.Scope))
            .OrderBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal)
            .ThenBy(x => GetOwnershipPriority(x.Source.Ownership))
            .ThenBy(x => GetScopePriority(x.Scope, targetTenantId))
            .ThenBy(x => x.Source.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .ToArray();

        var connections = new List<EffectiveIdentityProviderConnection>(candidates.Length);
        foreach (var group in candidates.GroupBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal))
        {
            var candidatesForKey = group.ToArray();
            var hasInheritedScopeCollision = candidatesForKey.Select(x => x.Scope).Distinct().Skip(1).Any();

            for (var index = 0; index < candidatesForKey.Length; index++)
            {
                var candidate = candidatesForKey[index];
                connections.Add(new EffectiveIdentityProviderConnection(
                    candidate.Connection,
                    candidate.Source.Ownership,
                    candidate.Scope,
                    hasInheritedScopeCollision ? ConnectionValidity.Invalid : ConnectionValidity.Unknown,
                    !hasInheritedScopeCollision && index > 0,
                    candidate.Source.Name));
            }
        }

        var orderedConnections = connections
            .OrderBy(x => GetScopePriority(x.Scope, targetTenantId))
            .ThenBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .ToArray();

        var version = revisionCalculator.CalculateRegistryVersion(snapshots.Select(x => (x.Source.Name, x.Source.Ownership, x.Snapshot)));
        var loginMethods = CreateLoginMethods(orderedConnections, targetTenantId);
        return new EffectiveConnectionRegistry(orderedConnections, loginMethods, version);
    }

    public async ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(string targetTenantId, string key, CancellationToken cancellationToken = default)
    {
        var registry = await GetAsync(targetTenantId, cancellationToken);
        var normalizedKey = ConnectionRevisionCalculator.NormalizeKey(key);
        return registry.Connections.FirstOrDefault(x =>
            !x.IsShadowed &&
            x.Validity != ConnectionValidity.Invalid &&
            string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), normalizedKey, StringComparison.Ordinal));
    }

    public async ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(string targetTenantId, string connectionId, CancellationToken cancellationToken = default)
    {
        var registry = await GetAsync(targetTenantId, cancellationToken);
        return registry.Connections.FirstOrDefault(x => string.Equals(x.Connection.Id, connectionId, StringComparison.Ordinal));
    }

    private static IReadOnlyCollection<LoginMethod> CreateLoginMethods(IReadOnlyCollection<EffectiveIdentityProviderConnection> connections, string targetTenantId)
    {
        var available = connections
            .Where(x => !x.IsShadowed && x.Validity != ConnectionValidity.Invalid && x.Connection.IsEnabled && !x.Connection.ArchivedAt.HasValue)
            .ToArray();
        var targetScope = GetTargetScope(targetTenantId);
        var defaultScope = available.Any(x => x.Scope == targetScope && x.Connection.IsDefault)
            ? targetScope
            : ConnectionScope.Host;
        var defaultConnectionId = available
            .Where(x => x.Scope == defaultScope && x.Connection.IsDefault)
            .OrderBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .Select(x => x.Connection.Id)
            .FirstOrDefault();

        return available
            .OrderBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .Select(x => new LoginMethod(
                x.Connection.Id,
                x.Connection.Key,
                LoginMethodKind.External,
                x.Connection.DisplayName,
                x.Connection.IconId,
                x.Connection.DisplayOrder,
                string.Equals(x.Connection.Id, defaultConnectionId, StringComparison.Ordinal),
                new Uri($"/external-authentication/authorize/{Uri.EscapeDataString(x.Connection.Key)}", UriKind.Relative)))
            .ToArray();
    }

    private static IReadOnlyList<ConnectionScope> GetApplicableScopes(string? targetTenantId)
    {
        if (targetTenantId is null)
            return [ConnectionScope.Host];

        var targetScope = GetTargetScope(targetTenantId);
        return targetScope == ConnectionScope.Host ? [ConnectionScope.Host] : [targetScope, ConnectionScope.Host];
    }

    private static ConnectionScope GetTargetScope(string targetTenantId)
    {
        if (targetTenantId == ConnectionScope.HostTenantId)
            return ConnectionScope.Host;

        return targetTenantId.Length == 0
            ? ConnectionScope.DefaultTenant
            : new ConnectionScope(ConnectionScopeKind.Tenant, targetTenantId);
    }

    private static bool IsInScope(IdentityProviderConnection connection, ConnectionScope scope) => string.Equals(connection.TenantId, scope.TenantId, StringComparison.Ordinal);
    private static int GetOwnershipPriority(ConnectionSourceOwnership ownership) => ownership == ConnectionSourceOwnership.Configuration ? 0 : 1;
    private static int GetScopePriority(ConnectionScope scope, string? targetTenantId) => targetTenantId is not null && scope.TenantId == targetTenantId ? 0 : 1;

    private sealed record Candidate(IIdentityProviderConnectionSource Source, ConnectionScope Scope, IdentityProviderConnection Connection);
}
