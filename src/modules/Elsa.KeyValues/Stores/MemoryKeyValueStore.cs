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

    private void ApplyCurrentTenant(Entity entity)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId || _tenantAccessor is null)
            return;

        entity.TenantId ??= _tenantAccessor.TenantId;
    }
}
