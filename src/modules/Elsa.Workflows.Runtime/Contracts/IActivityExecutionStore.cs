using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Stores activity execution records.
/// </summary>
public interface IActivityExecutionStore
{
    /// <summary>
    /// Saves the specified activity execution record.
    /// </summary>
    /// <param name="record">The activity execution record.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the specified activity execution records.
    /// </summary>
    /// <param name="records">The activity execution records.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds all activity execution records matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use when finding activity execution records.</param>
    /// <param name="order">The order to use when finding activity execution records.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the order by field.</typeparam>
    /// <returns>A collection of activity execution records.</returns>
    Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds all activity execution records matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use when finding activity execution records.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of activity execution records.</returns>
    Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all activity execution records matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use when counting activity execution records.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of activity execution records matching the specified filter.</returns>
    Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default);
}