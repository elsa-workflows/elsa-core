using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// A Dapper implementation of <see cref="IKeyValueStore"/>.
/// </summary>
public class DapperKeyValueStore : IKeyValueStore
{
    private const string TableName = "Users";
    private const string PrimaryKeyName = "Key";
    private readonly Store<KeyValuePairRecord> _store;
    
    /// <summary>
    /// Initializes a new instance of <see cref="DapperKeyValueStore"/>.
    /// </summary>
    public DapperKeyValueStore(IDbConnectionProvider dbConnectionProvider)
    {
        _store = new Store<KeyValuePairRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        var record = Map(keyValuePair);
        return _store.SaveAsync(record, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : Map(record);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return records.Select(Map);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return _store.DeleteAsync(query => query.Is(nameof(KeyValuePairRecord.Key), key), cancellationToken);
    }

    private void ApplyFilter(ParameterizedQuery query, KeyValueFilter filter)
    {
        query
            .Is(nameof(KeyValuePairRecord.Key), filter.Key)
            .In(nameof(KeyValuePairRecord.Key), filter.Keys)
            .StartsWith(nameof(KeyValuePairRecord.Key), filter.StartsWith, filter.Key);
    }

    private KeyValuePairRecord Map(SerializedKeyValuePair kvp)
    {
        return new()
        {
            Key = kvp.Key,
            Value = kvp.SerializedValue
        };
    }

    private SerializedKeyValuePair Map(KeyValuePairRecord kvp)
    {
        return new()
        {
            Key = kvp.Key,
            SerializedValue = kvp.Value
        };
    }
}