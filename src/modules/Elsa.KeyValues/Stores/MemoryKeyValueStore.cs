using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;

namespace Elsa.KeyValues.Stores;

/// <summary>
/// Stores key value records in memory.
/// </summary>
/// <remarks>
/// Ambient tenant is applied here rather than in callers.
/// EF owns that via <c>SetTenantIdFilter</c> / <c>ApplyTenantId</c>; Memory must compensate.
/// Key-value contracts have no TenantAgnostic flag, so isolation always applies (EF query filter).
/// <see cref="SerializedKeyValuePair"/> is keyed by <c>Id</c> (= <c>Key</c>) alone; <c>TenantId</c>
/// is a filter, not part of the primary key. Callers that need tenant-scoped names must encode
/// the tenant into the key. A composite <c>(TenantId, Key)</c> identity is out of scope.
/// Save uses <see cref="TenantVisibility.CanReplaceOwnedRow"/> so a named tenant cannot take over
/// another tenant's key or a <c>*</c> key (EF: PK collision). Delete stays visibility-filtered.
/// </remarks>
public class MemoryKeyValueStore : IKeyValueStore
{
    private readonly MemoryStore<SerializedKeyValuePair> _store;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryKeyValueStore"/> class.
    /// </summary>
    public MemoryKeyValueStore(MemoryStore<SerializedKeyValuePair> store, ITenantAccessor? tenantAccessor = null)
    {
        _store = store;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        lock (_store.Sync)
        {
            ApplyCurrentTenant(keyValuePair);
            EnsureKeyAvailable(keyValuePair);
            _store.Save(keyValuePair, kv => kv.Id);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var result = _store.Query(query => filter.Apply(query.WhereVisibleToTenant(CurrentTenantId))).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var result = _store.Query(query => filter.Apply(query.WhereVisibleToTenant(CurrentTenantId)));
        return Task.FromResult(result);
    }
    
    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        lock (_store.Sync)
            _store.DeleteWhere(x => x.Key == key && IsVisible(x));

        return Task.CompletedTask;
    }

    private bool IsVisible(Entity entity) => TenantVisibility.IsVisible(entity.TenantId, CurrentTenantId);

    private string CurrentTenantId => _tenantAccessor?.TenantId ?? Tenant.DefaultTenantId;

    private void EnsureKeyAvailable(SerializedKeyValuePair incoming)
    {
        if (_tenantAccessor is null)
            return;

        var existing = _store.Find(x => x.Id == incoming.Id);

        if (existing is null)
            return;

        if (!TenantVisibility.CanReplaceOwnedRow(existing.TenantId, incoming.TenantId, CurrentTenantId))
            throw new InvalidOperationException($"A key-value pair with key '{incoming.Id}' already exists and is not visible to the current tenant.");

        // An accepted update may change the payload, but it must not rehome the row.
        incoming.TenantId = existing.TenantId;
    }

    private void ApplyCurrentTenant(Entity entity)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId || _tenantAccessor is null)
            return;

        entity.TenantId ??= _tenantAccessor.TenantId;
    }
}
