using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

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
    public Task<SerializedKeyValuePair?> GetValue(string key, CancellationToken cancellationToken)
    {
        var result = _store.Find(x => x.Key == key);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        _store.DeleteWhere(x => x.Key == key);
        return Task.CompletedTask;
    }
}