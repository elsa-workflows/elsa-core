using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Contracts;

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
    /// Returns all records matching the specified filter.
    /// </summary>
    ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a set of records based on the specified removed and added records.
    /// </summary>
    ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all records matching the specified filter.
    /// </summary>
    ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default);
}