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
        var scopes = GetApplicableScopes();
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
            .ThenBy(x => x.Source.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .ToArray();

        var connections = new List<EffectiveIdentityProviderConnection>(candidates.Length);
        foreach (var group in candidates.GroupBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal))
        {
            var candidatesForKey = group.ToArray();
            var hasInheritedScopeCollision = candidatesForKey.Select(x => x.Scope).Distinct().Skip(1).Any();

            var explicitOverride = candidatesForKey.FirstOrDefault(x => x.Source.Ownership == ConnectionSourceOwnership.Database && x.Connection.OverridesConfigurationConnection && !x.Connection.ArchivedAt.HasValue);
            var preferred = explicitOverride ?? candidatesForKey.FirstOrDefault(x => x.Source.Ownership == ConnectionSourceOwnership.Configuration) ?? candidatesForKey.First();
            var preferredReference = ToReference(preferred);
            var shadowedReferences = hasInheritedScopeCollision
                ? []
                : candidatesForKey
                    .Where(candidate => !ReferenceEquals(candidate, preferred) && !candidate.Connection.ArchivedAt.HasValue)
                    .Select(ToReference)
                    .ToArray();

            for (var index = 0; index < candidatesForKey.Length; index++)
            {
                var candidate = candidatesForKey[index];
                var isArchived = candidate.Connection.ArchivedAt.HasValue;
                var isShadowed = !isArchived && !hasInheritedScopeCollision && !ReferenceEquals(candidate, preferred);
                connections.Add(new EffectiveIdentityProviderConnection(
                    candidate.Connection,
                    candidate.Source.Ownership,
                    candidate.Scope,
                    hasInheritedScopeCollision ? ConnectionValidity.Invalid : ConnectionValidity.Unknown,
                    isShadowed,
                    candidate.Source.Name)
                {
                    ShadowedBy = isShadowed ? preferredReference : null,
                    Shadows = isShadowed || isArchived ? [] : shadowedReferences
                });
            }
        }

        var orderedConnections = connections
            .OrderBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .ToArray();

        var version = revisionCalculator.CalculateRegistryVersion(snapshots.Select(x => (x.Source.Name, x.Source.Ownership, x.Snapshot)));
        var loginMethods = CreateLoginMethods(orderedConnections);
        return new EffectiveConnectionRegistry(orderedConnections, loginMethods, version);
    }

    public async ValueTask<EffectiveIdentityProviderConnection?> FindByKeyAsync(string targetTenantId, string key, CancellationToken cancellationToken = default)
    {
        var registry = await GetAsync(targetTenantId, cancellationToken);
        var normalizedKey = ConnectionRevisionCalculator.NormalizeKey(key);
        return registry.Connections.FirstOrDefault(x =>
            IsAvailableForAuthentication(x) &&
            string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), normalizedKey, StringComparison.Ordinal));
    }

    public async ValueTask<EffectiveIdentityProviderConnection?> FindByIdAsync(string targetTenantId, string connectionId, CancellationToken cancellationToken = default)
    {
        var registry = await GetAsync(targetTenantId, cancellationToken);
        return registry.Connections.FirstOrDefault(x => string.Equals(x.Connection.Id, connectionId, StringComparison.Ordinal));
    }

    private static IReadOnlyCollection<LoginMethod> CreateLoginMethods(IReadOnlyCollection<EffectiveIdentityProviderConnection> connections)
    {
        var available = connections
            .Where(IsAvailableForAuthentication)
            .ToArray();
        var configuredPreferred = available
            .Where(x => x.Ownership == ConnectionSourceOwnership.Configuration && x.Connection.IsPreferred)
            .OrderBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        if (configuredPreferred is not null)
            return ToLoginMethods(available, configuredPreferred.Connection.Id);

        var preferredConnectionId = available
            .Where(x => x.Connection.IsPreferred)
            .OrderBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .Select(x => x.Connection.Id)
            .FirstOrDefault();

        return ToLoginMethods(available, preferredConnectionId);
    }

    private static IReadOnlyCollection<LoginMethod> ToLoginMethods(IEnumerable<EffectiveIdentityProviderConnection> connections, string? preferredConnectionId) => connections
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
                string.Equals(x.Connection.Id, preferredConnectionId, StringComparison.Ordinal),
                new Uri($"/external-authentication/authorize/{Uri.EscapeDataString(x.Connection.Key)}", UriKind.Relative)))
            .ToArray();

    private static IReadOnlyList<ConnectionScope> GetApplicableScopes() => [ConnectionScope.Host];

    private static bool IsInScope(IdentityProviderConnection connection, ConnectionScope scope) => string.Equals(connection.TenantId, scope.TenantId, StringComparison.Ordinal);
    private static bool IsAvailableForAuthentication(EffectiveIdentityProviderConnection connection) =>
        !connection.IsShadowed &&
        connection.Validity != ConnectionValidity.Invalid &&
        connection.Connection.IsEnabled &&
        !connection.Connection.ArchivedAt.HasValue;
    private static int GetOwnershipPriority(ConnectionSourceOwnership ownership) => ownership == ConnectionSourceOwnership.Configuration ? 0 : 1;
    private static IdentityProviderConnectionReference ToReference(Candidate candidate) =>
        new(candidate.Connection.Id, candidate.Connection.DisplayName, candidate.Source.Ownership);

    private sealed record Candidate(IIdentityProviderConnectionSource Source, ConnectionScope Scope, IdentityProviderConnection Connection);
}
