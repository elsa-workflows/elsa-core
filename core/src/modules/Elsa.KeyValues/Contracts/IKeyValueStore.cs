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

    /// <summary>
    /// Deletes the record if the key exists.
    /// </summary>
    /// <returns><c>true</c> if a row was removed; <c>false</c> if the key was already absent.</returns>
    /// <remarks>
    /// The default implementation is not concurrency-safe: two concurrent callers can both get
    /// <c>true</c> because it finds by key and then calls <see cref="DeleteAsync"/>. Shared stores
    /// must override this with a single count-checked delete. Decorators must forward
    /// <see cref="TryDeleteAsync"/>, or they fall back to this non-atomic default.
    /// </remarks>
    async Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var existing = await FindAsync(new KeyValueFilter { Key = key }, cancellationToken);
        if (existing is null)
            return false;

        await DeleteAsync(key, cancellationToken);
        return true;
    }
}