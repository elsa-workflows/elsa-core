using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Provides extension methods for <see cref="IActivityExecutionStore"/>.
/// </summary>
public static class ActivityExecutionStoreExtensions
{
    /// <summary>
    /// Retrieves the execution chain for the specified activity execution record by traversing the SchedulingActivityExecutionId chain.
    /// Returns records ordered from root (depth 0) to the specified record.
    /// </summary>
    public static async Task<Page<ActivityExecutionRecord>> GetExecutionChainAsync(
        this IActivityExecutionStore store,
        string activityExecutionId,
        bool includeCrossWorkflowChain = true,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var chain = new List<ActivityExecutionRecord>();
        var visited = new HashSet<string>();
        var loadedRecords = new Dictionary<string, ActivityExecutionRecord>();
        var currentId = activityExecutionId;

        while (currentId != null && visited.Add(currentId))
        {
            if (!loadedRecords.TryGetValue(currentId, out var record))
            {
                record = await store.FindAsync(new() { Id = currentId }, cancellationToken);
                
                if (record == null)
                    break;

                loadedRecords.TryAdd(currentId, record);
            }

            chain.Add(record);

            if (!includeCrossWorkflowChain && record.SchedulingWorkflowInstanceId != null)
                break;

            currentId = record.SchedulingActivityExecutionId;
        }

        chain.Reverse();

        var totalCount = chain.Count;

        if (skip.HasValue)
            chain = chain.Skip(skip.Value).ToList();
        
        if (take.HasValue)
            chain = chain.Take(take.Value).ToList();

        return Page.Of(chain, totalCount);
    }
}
