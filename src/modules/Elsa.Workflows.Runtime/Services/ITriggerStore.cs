using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Provides access to the underlying store of stored triggers.
/// </summary>
public interface ITriggerStore
{
    /// <summary>
    /// Adds or updates the specified record.
    /// </summary>
    ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds or updates the specified set of records. 
    /// </summary>
    ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns all records matching the specified hash value.
    /// </summary>
    ValueTask<IEnumerable<StoredTrigger>> FindAsync(string hash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns all records matching the specified workflow definition ID. 
    /// </summary>
    ValueTask<IEnumerable<StoredTrigger>> FindByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Replaces a set of records based on the specified removed and added records.
    /// </summary>
    ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all records with the specified set of IDs.
    /// </summary>
    ValueTask DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a set of records matching the specified activity type.
    /// </summary>
    ValueTask<IEnumerable<StoredTrigger>> FindByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default);
}