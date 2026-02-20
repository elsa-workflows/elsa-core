using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A specification to use when finding activity execution log records. Only non-null fields will be included in the conditional expression.
/// </summary>
public class ActivityExecutionRecordFilter
{
    /// <summary>
    /// The ID of the activity execution record.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The IDs of the activity execution records.
    /// </summary>
    public ICollection<string>? Ids { get; set; }

    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The IDs of the workflow instances.
    /// </summary>
    public ICollection<string>? WorkflowInstanceIds { get; set; }

    /// <summary>
    /// The ID of the activity.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// The IDs of the activities.
    /// </summary>
    public ICollection<string>? ActivityIds { get; set; }

    /// <summary>
    /// The node ID of the activity.
    /// </summary>
    public string? ActivityNodeId { get; set; }

    /// <summary>
    /// The node IDs of the activities.
    /// </summary>
    public ICollection<string>? ActivityNodeIds { get; set; }

    /// <summary>
    /// The name of the activity.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The names of the activities.
    /// </summary>
    public ICollection<string>? Names { get; set; }

    /// <summary>
    /// The status of the activity.
    /// </summary>
    public ActivityStatus? Status { get; set; }

    /// <summary>
    /// The statuses of the activities.
    /// </summary>
    public ICollection<ActivityStatus>? Statuses { get; set; }

    /// <summary>
    /// Whether to include completed activity execution records. If not specified, all activity execution records will be included.
    /// </summary>
    public bool? Completed { get; set; }

    /// <summary>
    /// The ID of the activity execution context that scheduled this activity execution.
    /// </summary>
    public string? SchedulingActivityExecutionId { get; set; }

    /// <summary>
    /// The IDs of the activity execution contexts that scheduled activity executions.
    /// </summary>
    public ICollection<string>? SchedulingActivityExecutionIds { get; set; }

    /// <summary>
    /// The ID of the activity that scheduled this activity execution (denormalized).
    /// </summary>
    public string? SchedulingActivityId { get; set; }

    /// <summary>
    /// The IDs of the activities that scheduled activity executions (denormalized).
    /// </summary>
    public ICollection<string>? SchedulingActivityIds { get; set; }

    /// <summary>
    /// The workflow instance ID of the workflow that scheduled this activity execution.
    /// Used for cross-workflow call stack tracking.
    /// </summary>
    public string? SchedulingWorkflowInstanceId { get; set; }

    /// <summary>
    /// The workflow instance IDs of the workflows that scheduled activity executions.
    /// </summary>
    public ICollection<string>? SchedulingWorkflowInstanceIds { get; set; }

    /// <summary>
    /// The call stack depth of the activity execution.
    /// </summary>
    public int? CallStackDepth { get; set; }

    /// <summary>
    /// Returns true if the filter is empty.
    /// </summary>
    public bool IsEmpty =>
        Id == null
        && Ids == null
        && WorkflowInstanceId == null
        && WorkflowInstanceIds == null
        && ActivityId == null
        && ActivityIds == null
        && ActivityNodeId == null
        && ActivityNodeIds == null
        && Name == null
        && Names == null
        && Status == null
        && Statuses == null
        && Completed == null
        && SchedulingActivityExecutionId == null
        && SchedulingActivityExecutionIds == null
        && SchedulingActivityId == null
        && SchedulingActivityIds == null
        && SchedulingWorkflowInstanceId == null
        && SchedulingWorkflowInstanceIds == null
        && CallStackDepth == null;

    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    public IQueryable<ActivityExecutionRecord> Apply(IQueryable<ActivityExecutionRecord> queryable)
    {
        var filter = this;
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id));
        if (filter.WorkflowInstanceId != null) queryable = queryable.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);
        if (filter.WorkflowInstanceIds != null) queryable = queryable.Where(x => filter.WorkflowInstanceIds.Contains(x.WorkflowInstanceId));
        if (filter.ActivityId != null) queryable = queryable.Where(x => x.ActivityId == filter.ActivityId);
        if (filter.ActivityIds != null && filter.ActivityIds.Any()) queryable = queryable.Where(x => filter.ActivityIds.Contains(x.ActivityId));
        if (filter.ActivityNodeId != null) queryable = queryable.Where(x => x.ActivityNodeId == filter.ActivityNodeId);
        if (filter.ActivityNodeIds != null && filter.ActivityNodeIds.Any()) queryable = queryable.Where(x => filter.ActivityNodeIds.Contains(x.ActivityNodeId));
        if (filter.Name != null) queryable = queryable.Where(x => x.ActivityName == filter.Name);
        if (filter.Names != null && filter.Names.Any()) queryable = queryable.Where(x => filter.Names.Contains(x.ActivityName!));
        if (filter.Status != null) queryable = queryable.Where(x => x.Status == filter.Status);
        if (filter.Statuses != null && filter.Statuses.Any()) queryable = queryable.Where(x => filter.Statuses.Contains(x.Status));
        if (filter.Completed != null) queryable = filter.Completed == true ? queryable.Where(x => x.CompletedAt != null) : queryable.Where(x => x.CompletedAt == null);
        if (filter.SchedulingActivityExecutionId != null) queryable = queryable.Where(x => x.SchedulingActivityExecutionId == filter.SchedulingActivityExecutionId);
        if (filter.SchedulingActivityExecutionIds != null && filter.SchedulingActivityExecutionIds.Any()) queryable = queryable.Where(x => x.SchedulingActivityExecutionId != null && filter.SchedulingActivityExecutionIds.Contains(x.SchedulingActivityExecutionId));
        if (filter.SchedulingActivityId != null) queryable = queryable.Where(x => x.SchedulingActivityId == filter.SchedulingActivityId);
        if (filter.SchedulingActivityIds != null && filter.SchedulingActivityIds.Any()) queryable = queryable.Where(x => x.SchedulingActivityId != null && filter.SchedulingActivityIds.Contains(x.SchedulingActivityId));
        if (filter.SchedulingWorkflowInstanceId != null) queryable = queryable.Where(x => x.SchedulingWorkflowInstanceId == filter.SchedulingWorkflowInstanceId);
        if (filter.SchedulingWorkflowInstanceIds != null && filter.SchedulingWorkflowInstanceIds.Any()) queryable = queryable.Where(x => x.SchedulingWorkflowInstanceId != null && filter.SchedulingWorkflowInstanceIds.Contains(x.SchedulingWorkflowInstanceId));
        if (filter.CallStackDepth != null) queryable = queryable.Where(x => x.CallStackDepth == filter.CallStackDepth);

        return queryable;
    }
}