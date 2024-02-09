using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// Stores key value records in memory.
/// </summary>
public class MemoryKeyValueStore : IKeyValueStore
{
    private readonly MemoryStore<SerializedKeyValuePair> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryActivityExecutionStore"/> class.
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