using Elsa.Workflows.Runtime.Entities;
 
namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Store that holds key value entities not fit to store in specific stores.
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
    Task<SerializedKeyValuePair?> GetValue(string key, CancellationToken cancellationToken);
    
    /// <summary>
    /// If the key is found it deletes the record from the store. 
    /// </summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}