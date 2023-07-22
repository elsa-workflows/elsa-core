using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A specification to use when finding activity execution log records. Only non-null fields will be included in the conditional expression.
/// </summary>
public class ActivityExecutionRecordFilter
{
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }
    
    /// <summary>
    /// The ID of the activity.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// The IDs of the activities.
    /// </summary>
    public ICollection<string>? ActivityIds { get; set; }

    /// <summary>
    /// Whether to include completed activity execution records. If not specified, all activity execution records will be included.
    /// </summary>
    public bool? Completed { get; set; }
    
    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    public IQueryable<ActivityExecutionRecord> Apply(IQueryable<ActivityExecutionRecord> queryable)
    {
        var filter = this;
        if (filter.WorkflowInstanceId != null) queryable = queryable.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);
        if (filter.ActivityId != null) queryable = queryable.Where(x => x.ActivityId == filter.ActivityId);
        if (filter.ActivityIds != null && filter.ActivityIds.Any()) queryable = queryable.Where(x => filter.ActivityIds.Contains(x.ActivityId));
        if (filter.Completed != null) queryable = filter.Completed == true ? queryable.Where(x => x.CompletedAt != null) : queryable.Where(x => x.CompletedAt == null);

        return queryable;
    }
}