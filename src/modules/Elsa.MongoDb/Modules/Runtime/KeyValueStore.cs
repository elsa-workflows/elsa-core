using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.MongoDb.Modules.Runtime;

/// <summary>
/// A MongoDB based store for <see cref="SerializedKeyValuePair"/>s.
/// </summary>
public class MongoKeyValueStore : IKeyValueStore
{
    private readonly MongoDbStore<SerializedKeyValuePair> _keyValueMongoDbStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueStore"/> class.
    /// </summary>
    public MongoKeyValueStore(MongoDbStore<SerializedKeyValuePair> keyValueMongoDbStore)
    {
        _keyValueMongoDbStore = keyValueMongoDbStore;
    }

    /// <inheritdoc />
    public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        return _keyValueMongoDbStore.SaveAsync(keyValuePair, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SerializedKeyValuePair?> GetValue(string key, CancellationToken cancellationToken)
    {
        return _keyValueMongoDbStore.FindAsync(x => x.Key == key, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return _keyValueMongoDbStore.DeleteWhereAsync(x => x.Key == key, cancellationToken);
    }
}