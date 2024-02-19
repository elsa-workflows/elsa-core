using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.MongoDb.Common;
using MongoDB.Driver.Linq;
using Open.Linq.AsyncExtensions;

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
    public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        return _keyValueMongoDbStore.FindAsync(query => Filter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        return await _keyValueMongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken).ToList();
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return _keyValueMongoDbStore.DeleteWhereAsync(x => x.Key == key, cancellationToken);
    }

    private IMongoQueryable<SerializedKeyValuePair> Filter(IMongoQueryable<SerializedKeyValuePair> queryable, KeyValueFilter filter) =>
        (filter.Apply(queryable) as IMongoQueryable<SerializedKeyValuePair>)!;
}