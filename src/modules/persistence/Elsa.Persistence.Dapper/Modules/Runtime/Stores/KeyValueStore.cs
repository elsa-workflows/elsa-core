using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Models;
using Elsa.Persistence.Dapper.Modules.Runtime.Records;
using Elsa.Persistence.Dapper.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using JetBrains.Annotations;

namespace Elsa.Persistence.Dapper.Modules.Runtime.Stores;

/// <summary>
/// A Dapper implementation of <see cref="IKeyValueStore"/>.
/// </summary>
[UsedImplicitly]
internal class DapperKeyValueStore(Store<KeyValuePairRecord> store) : IKeyValueStore
{
    /// <inheritdoc />
    public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        var record = Map(keyValuePair);
        return store.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return records.Select(Map);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return store.DeleteAsync(query => query.Is(nameof(KeyValuePairRecord.Id), key), cancellationToken);
    }

    /// <summary>
    /// Atomic delete: returns true only when the DELETE affected a row.
    /// Becomes <c>IKeyValueStore.TryDeleteAsync</c> once ElsaVersion includes elsa-core#8538.
    /// </summary>
    public async Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(query => query.Is(nameof(KeyValuePairRecord.Id), key), cancellationToken) > 0;
    }

    private void ApplyFilter(ParameterizedQuery query, KeyValueFilter filter)
    {
        if (filter.StartsWith)
            query.StartsWith(nameof(KeyValuePairRecord.Id), true, filter.Key);
        else
            query.Is(nameof(KeyValuePairRecord.Id), filter.Key);

        query.In(nameof(KeyValuePairRecord.Id), filter.Keys);
    }

    private KeyValuePairRecord Map(SerializedKeyValuePair source)
    {
        return new()
        {
            Id = source.Id,
            Value = source.SerializedValue,
            TenantId = source.TenantId
        };
    }

    private SerializedKeyValuePair Map(KeyValuePairRecord source)
    {
        return new()
        {
            Id = source.Id,
            SerializedValue = source.Value,
            TenantId = source.TenantId
        };
    }
}