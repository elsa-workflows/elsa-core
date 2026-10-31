using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// <summary>
/// A specification to use when finding workflow execution log records. Only non-null fields will be included in the conditional expression.
/// </summary>
public class WorkflowExecutionLogRecordFilter
{
    /// <summary>
    /// The ID of the workflow execution log record.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// The IDs of the workflow execution log records.
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
    /// The ID of the parent activity instance.
    /// </summary>
    public string? ParentActivityInstanceId { get; set; }
    
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
    /// The activity type to exclude.
    /// </summary>
    public string? ExcludeActivityType { get; set; }
    
    /// <summary>
    /// The activity types to exclude.
    /// </summary>
    public ICollection<string>? ExcludeActivityTypes { get; set; }
    
    /// <summary>
    /// The name of the event.
    /// </summary>
    public string? EventName { get; set; }
    
    /// <summary>
    /// Match all of these event names.
    /// </summary>
    public ICollection<string>? EventNames { get; set; }
    
    /// <summary>
    /// Applies the filter to the specified queryable.
    /// </summary>
    public IQueryable<WorkflowExecutionLogRecord> Apply(IQueryable<WorkflowExecutionLogRecord> queryable)
    {
        var filter = this;
        if(filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id!));
        if (filter.WorkflowInstanceId != null) queryable = queryable.Where(x => x.WorkflowInstanceId == filter.WorkflowInstanceId);
        if (filter.WorkflowInstanceIds != null) queryable = queryable.Where(x => filter.WorkflowInstanceIds.Contains(x.WorkflowInstanceId!));
        if (filter.ParentActivityInstanceId != null) queryable = queryable.Where(x => x.ParentActivityInstanceId == filter.ParentActivityInstanceId);
        if (filter.ActivityId != null) queryable = queryable.Where(x => x.ActivityId == filter.ActivityId);
        if (filter.ActivityIds != null) queryable = queryable.Where(x => filter.ActivityIds.Contains(x.ActivityId));
        if (filter.ActivityNodeId != null) queryable = queryable.Where(x => x.ActivityNodeId == filter.ActivityNodeId);
        if (filter.ActivityNodeIds != null) queryable = queryable.Where(x => filter.ActivityNodeIds.Contains(x.ActivityNodeId));
        if (filter.EventName != null) queryable = queryable.Where(x => x.EventName == filter.EventName);
        if (filter.EventNames != null) queryable = queryable.Where(x => filter.EventNames.Contains(x.EventName!));
        if(filter.ExcludeActivityType != null) queryable = queryable.Where(x => x.ActivityType != filter.ExcludeActivityType);
        if(filter.ExcludeActivityTypes != null) queryable = queryable.Where(x => !filter.ExcludeActivityTypes.Contains(x.ActivityType));
        
        return queryable;
    }
}