using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class ActivityExecutionStatsService : IActivityExecutionStatsService
{
    private readonly IActivityExecutionStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityExecutionStatsService"/> class.
    /// </summary>
    public ActivityExecutionStatsService(IActivityExecutionStore store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionStats>> GetStatsAsync(string workflowInstanceId, IEnumerable<string> activityNodeIds, CancellationToken cancellationToken = default)
    {
        var filter = new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityNodeIds = activityNodeIds?.ToList()
        };
        var order = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        var records = (await _store.FindManyAsync(filter, order, cancellationToken)).ToList();
        var groupedRecords = records.GroupBy(x => x.ActivityNodeId).ToList();
        var stats = groupedRecords.Select(grouping => new ActivityExecutionStats
        {
            ActivityNodeId = grouping.Key,
            ActivityId = grouping.First().ActivityId,
            StartedCount = grouping.Count(),
            CompletedCount = grouping.Count(x => x.CompletedAt != null),
            UncompletedCount = grouping.Count(x => x.CompletedAt == null),
            IsBlocked = grouping.Any(x => x.HasBookmarks),
            IsFaulted = grouping.Any(x => x.Status == ActivityStatus.Faulted)
        }).ToList();
        
        return stats;
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionStats> GetStatsAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default)
    {
        var stats = (await GetStatsAsync(workflowInstanceId, new[] { activityNodeId }, cancellationToken)).FirstOrDefault();
        
        return stats ?? new ActivityExecutionStats
        {
            ActivityNodeId = activityNodeId
        };
    }
}