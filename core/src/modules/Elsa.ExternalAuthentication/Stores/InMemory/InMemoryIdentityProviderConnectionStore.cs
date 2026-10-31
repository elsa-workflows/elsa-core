using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.Stores.InMemory;

/// <summary>
/// Single-node default connection store. Durable hosts replace this registration with their persistence implementation.
/// </summary>
public sealed class InMemoryIdentityProviderConnectionStore : IIdentityProviderConnectionStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, IdentityProviderConnection> _connections = new(StringComparer.Ordinal);

    public ValueTask<Page<IdentityProviderConnection>> FindAsync(ConnectionFilter filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            IEnumerable<IdentityProviderConnection> query = _connections.Values;
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();
                query = query.Where(x => x.Key.Contains(search, StringComparison.OrdinalIgnoreCase) || x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.Scope is { } scope)
                query = query.Where(x => string.Equals(x.TenantId, scope.TenantId, StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(filter.AdapterType))
                query = query.Where(x => string.Equals(x.AdapterType, filter.AdapterType, StringComparison.Ordinal));
            if (filter.IsEnabled.HasValue)
                query = query.Where(x => x.IsEnabled == filter.IsEnabled.Value);
            if (filter.IsArchived.HasValue)
                query = query.Where(x => x.ArchivedAt.HasValue == filter.IsArchived.Value);

            var connections = query
                .OrderBy(x => x.TenantId, StringComparer.Ordinal)
                .ThenBy(x => x.DisplayOrder)
                .ThenBy(x => x.Key, StringComparer.Ordinal)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(IdentityProviderConnectionCloner.Clone)
                .ToArray();
            return ValueTask.FromResult(Page.Of<IdentityProviderConnection>(connections, connections.Length));
        }
    }

    public ValueTask<IdentityProviderConnection?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
            return ValueTask.FromResult(_connections.TryGetValue(id, out var connection) ? IdentityProviderConnectionCloner.Clone(connection) : null);
    }

    public ValueTask<ConnectionMutationResult> CreateAsync(IdentityProviderConnection connection, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_connections.ContainsKey(connection.Id) || _connections.Values.Any(x => string.Equals(x.TenantId, connection.TenantId, StringComparison.Ordinal) && string.Equals(x.Key, connection.Key, StringComparison.Ordinal)))
                return ValueTask.FromResult<ConnectionMutationResult>(new ConnectionMutationResult.DuplicateKey());

            var stored = IdentityProviderConnectionCloner.Clone(connection);
            stored.Revision = 1;
            _connections.Add(stored.Id, stored);
            return ValueTask.FromResult<ConnectionMutationResult>(new ConnectionMutationResult.Created(IdentityProviderConnectionCloner.Clone(stored)));
        }
    }

    public ValueTask<ConnectionMutationResult> UpdateAsync(IdentityProviderConnection connection, long expectedRevision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (!_connections.TryGetValue(connection.Id, out var current))
                return ValueTask.FromResult<ConnectionMutationResult>(new ConnectionMutationResult.NotFound());
            if (current.Revision != expectedRevision)
                return ValueTask.FromResult<ConnectionMutationResult>(new ConnectionMutationResult.RevisionConflict(current.Revision));
            if (_connections.Values.Any(x => !string.Equals(x.Id, connection.Id, StringComparison.Ordinal) && string.Equals(x.TenantId, connection.TenantId, StringComparison.Ordinal) && string.Equals(x.Key, connection.Key, StringComparison.Ordinal)))
                return ValueTask.FromResult<ConnectionMutationResult>(new ConnectionMutationResult.DuplicateKey());

            var stored = IdentityProviderConnectionCloner.Clone(connection);
            stored.Revision = current.Revision + 1;
            _connections[stored.Id] = stored;
            return ValueTask.FromResult<ConnectionMutationResult>(new ConnectionMutationResult.Updated(IdentityProviderConnectionCloner.Clone(stored)));
        }
    }
}
