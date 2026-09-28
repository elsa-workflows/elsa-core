using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.Persistence.MongoDb.Common;
using JetBrains.Annotations;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Elsa.Persistence.MongoDb.Modules.Runtime;

/// <summary>
/// A MongoDB based store for <see cref="SerializedKeyValuePair"/>s.
/// </summary>
[UsedImplicitly]
public class MongoKeyValueStore(MongoDbStore<SerializedKeyValuePair> keyValueMongoDbStore) : IKeyValueStore
{
    /// <inheritdoc />
    public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        return keyValueMongoDbStore.SaveAsync(keyValuePair, x => x.Key, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        return keyValueMongoDbStore.FindAsync(query => Filter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        return keyValueMongoDbStore.FindManyAsync(query => Filter(query, filter), cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return keyValueMongoDbStore.DeleteWhereAsync(x => x.Key == key, cancellationToken);
    }

    /// <summary>
    /// Atomic delete: returns true only when <c>DeleteOneAsync</c> removed a document.
    /// Becomes <c>IKeyValueStore.TryDeleteAsync</c> once ElsaVersion includes elsa-core#8538.
    /// </summary>
    public async Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var filter = keyValueMongoDbStore.ApplyTenantScope(Builders<SerializedKeyValuePair>.Filter.Eq(x => x.Key, key));
        var result = await keyValueMongoDbStore.GetCollection().DeleteOneAsync(filter, cancellationToken);
        return result.DeletedCount > 0;
    }

    private IQueryable<SerializedKeyValuePair> Filter(IQueryable<SerializedKeyValuePair> queryable, KeyValueFilter filter)
    {
        return filter.Apply(queryable);
    }
}