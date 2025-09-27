using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using JetBrains.Annotations;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <summary>
/// Entity Framework implementation of the <see cref="IKeyValueStore"/>
/// </summary>
[UsedImplicitly]
public class EFCoreKeyValueStore(Store<RuntimeElsaDbContext, SerializedKeyValuePair> store) : IKeyValueStore
{
    /// <inheritdoc />
    public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        return store.SaveAsync(keyValuePair, x => x.Id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        return store.FindAsync(filter.Apply, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        return store.QueryAsync(filter.Apply, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return store.DeleteWhereAsync(x => x.Id == key, cancellationToken);
    }
}