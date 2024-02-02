using Elsa.EntityFrameworkCore.Common;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

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
    public Task<SerializedKeyValuePair?> GetValue(string key, CancellationToken cancellationToken)
    {
        return _store.FindAsync(x => x.Key == key, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        return _store.DeleteWhereAsync(x => x.Key == key, cancellationToken);
    }
}