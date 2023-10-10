using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Manages activity execution records.
/// </summary>
public interface IActivityExecutionManager
{
    /// <summary>
    /// Deletes the records matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use when deleting activity execution records.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of activity execution records deleted.</returns>
    Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the specified activity execution record.
    /// </summary>
    Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default);
}