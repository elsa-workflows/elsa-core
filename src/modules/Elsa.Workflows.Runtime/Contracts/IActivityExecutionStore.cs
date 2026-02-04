using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Stores activity execution records.
/// </summary>
public interface IActivityExecutionStore : ILogRecordStore<ActivityExecutionRecord>
{
    /// <summary>
    /// Adds or updates the specified <see cref="ActivityExecutionRecord"/> in the persistence store.
    /// </summary>
    /// <param name="record">The activity execution record.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <remarks>
    /// If the record does not already exist, it is added to the store; if it does exist, its existing entry is updated.
    /// </remarks>
    Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an activity execution record matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to apply when searching for the activity execution record.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The activity execution record that matches the filter, or null if no match is found.</returns>
    Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default);

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
    /// Finds all activity execution record matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use when finding activity execution records.</param>
    /// <param name="order">The order to use when finding activity execution records.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <typeparam name="TOrderBy">The type of the order by field.</typeparam>
    /// <returns>A collection of activity execution record summaries.</returns>
    Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds all activity execution records matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use when finding activity execution records.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of activity execution record summaries.</returns>
    Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all activity execution records matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use when counting activity execution records.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of activity execution records matching the specified filter.</returns>
    Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all activity execution records matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of deleted records.</returns>
    Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the execution chain for the specified activity execution record by traversing the SchedulingActivityExecutionId chain.
    /// Returns records ordered from root (depth 0) to the specified record.
    /// </summary>
    /// <param name="activityExecutionId">The ID of the activity execution record to retrieve the chain for.</param>
    /// <param name="includeCrossWorkflowChain">If true (default), follows SchedulingWorkflowInstanceId across workflow boundaries. If false, stops at workflow boundaries.</param>
    /// <param name="skip">The number of items to skip (for pagination). Applied after ordering from root to leaf.</param>
    /// <param name="take">The maximum number of items to return (for pagination).</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A paginated result containing the call stack records, ordered from root to leaf.</returns>
    Task<Page<ActivityExecutionRecord>> GetExecutionChainAsync(
        string activityExecutionId,
        bool includeCrossWorkflowChain = true,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return ActivityExecutionStoreExtensions.GetExecutionChainAsync(this, activityExecutionId, includeCrossWorkflowChain, skip, take, cancellationToken);
    }
}
