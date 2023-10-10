using Elsa.Common.Entities;
using Elsa.Workflows.Core;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.Runtime.Services;

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
    public async Task<IEnumerable<ActivityExecutionStats>> GetStatsAsync(string workflowInstanceId, IEnumerable<string> activityIds, CancellationToken cancellationToken = default)
    {
        var filter = new ActivityExecutionRecordFilter
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityIds = activityIds.ToList()
        };
        var order = new ActivityExecutionRecordOrder<DateTimeOffset>(x => x.StartedAt, OrderDirection.Ascending);
        var records = (await _store.FindManyAsync(filter, order, cancellationToken)).ToList();
        var groupedRecords = records.GroupBy(x => x.ActivityId).ToList();
        var stats = groupedRecords.Select(grouping => new ActivityExecutionStats
        {
            ActivityId = grouping.Key,
            StartedCount = grouping.Count(),
            CompletedCount = grouping.Count(x => x.CompletedAt != null),
            UncompletedCount = grouping.Count(x => x.CompletedAt == null),
            IsBlocked = grouping.Any(x => x.HasBookmarks),
            IsFaulted = grouping.Any(x => x.Status == ActivityStatus.Faulted)
        }).ToList();
        
        return stats;
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionStats> GetStatsAsync(string workflowInstanceId, string activityId, CancellationToken cancellationToken = default)
    {
        var stats = (await GetStatsAsync(workflowInstanceId, new[] { activityId }, cancellationToken)).FirstOrDefault();
        
        return stats ?? new ActivityExecutionStats
        {
            ActivityId = activityId
        };
    }
}