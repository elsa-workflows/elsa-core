using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ActivityExecutionRecord"/>.
/// </summary>
public static class ActivityExecutionRecordExtensions
{
    /// <summary>
    /// Retrieves the execution chain for the specified activity execution record by using the store.
    /// Returns records ordered from root (depth 0) to the specified record.
    /// </summary>
    /// <param name="record">The activity execution record to retrieve the chain for.</param>
    /// <param name="store">The activity execution store to use for retrieving the chain.</param>
    /// <param name="includeCrossWorkflowChain">If true (default), follows SchedulingWorkflowInstanceId across workflow boundaries.</param>
    /// <param name="skip">The number of items to skip (for pagination).</param>
    /// <param name="take">The maximum number of items to return (for pagination).</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A paginated result containing the call stack records, ordered from root to leaf.</returns>
    public static Task<PagedCallStackResult> GetExecutionChainAsync(
        this ActivityExecutionRecord record,
        IActivityExecutionStore store,
        bool includeCrossWorkflowChain = true,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        return store.GetExecutionChainAsync(record.Id, includeCrossWorkflowChain, skip, take, cancellationToken);
    }
}
