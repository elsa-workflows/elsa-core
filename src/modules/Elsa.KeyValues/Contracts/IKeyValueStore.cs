using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;

namespace Elsa.KeyValues.Contracts;

/// <summary>
/// Store that holds key value entities that can be used to store application data. 
/// </summary>
public interface IKeyValueStore
{
    /// <summary>
    /// Saves the key value pair.
    /// </summary>
    Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the key value pair from the store.
    /// </summary>
    /// <returns><see cref="SerializedKeyValuePair"/> if the key is found, otherwise null.</returns>
    Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all key value pairs which match the predicate.
    /// </summary>
    Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken);
    
    /// <summary>
    /// If the key is found it deletes the record from the store. 
    /// </summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}