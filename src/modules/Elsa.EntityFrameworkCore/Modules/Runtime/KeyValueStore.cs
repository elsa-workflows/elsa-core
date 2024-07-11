using Elsa.EntityFrameworkCore.Common;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// Entity Framework implementation of the <see cref="IKeyValueStore"/>
/// </summary>
public class EFCoreKeyValueStore : IKeyValueStore
{
    private readonly Store<RuntimeElsaDbContext, SerializedKeyValuePair> _store;

    public EFCoreKeyValueStore(Store<RuntimeElsaDbContext, SerializedKeyValuePair> store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken)
    {
        return _store.SaveAsync(keyValuePair, x => x.Key, cancellationToken);
    }
    
    /// <inheritdoc />
    public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        return _store.FindAsync(filter.Apply, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken)
    {
        return _store.QueryAsync(filter.Apply, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return _store.DeleteWhereAsync(x => x.Key == key, cancellationToken);
    }
}