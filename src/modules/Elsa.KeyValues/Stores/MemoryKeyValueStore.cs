using Elsa.Common.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;

namespace Elsa.KeyValues.Stores;

/// <summary>
/// Stores key value records in memory.
/// </summary>
public class MemoryKeyValueStore : IKeyValueStore
{
    private readonly MemoryStore<SerializedKeyValuePair> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryKeyValueStore"/> class.
    /// </summary>
    public MemoryKeyValueStore(MemoryStore<SerializedKeyValuePair> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        _store.Save(keyValuePair, kv => kv.Key);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var result = _store.Query(filter.Apply).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        var result = _store.Query(filter.Apply);
        return Task.FromResult(result);
    }
    
    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _store.DeleteWhere(x => x.Key == key);
        return Task.CompletedTask;
    }
}