using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Runtime.Stores;

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
    public Task DeleteAsync(string key, CancellationToken cancellationToken) => store.DeleteAsync(query => query.Is(nameof(KeyValuePairRecord.Id), key), cancellationToken);

    private void ApplyFilter(ParameterizedQuery query, KeyValueFilter filter) =>
        query
            .Is(nameof(KeyValuePairRecord.Id), filter.Key)
            .In(nameof(KeyValuePairRecord.Id), filter.Keys)
            .StartsWith(nameof(KeyValuePairRecord.Id), filter.StartsWith, filter.Key);

    private KeyValuePairRecord Map(SerializedKeyValuePair source) =>
        new()
        {
            Id = source.Id,
            Value = source.SerializedValue,
            TenantId = source.TenantId
        };

    private SerializedKeyValuePair Map(KeyValuePairRecord source) =>
        new()
        {
            Id = source.Id,
            SerializedValue = source.Value,
            TenantId = source.TenantId
        };
}