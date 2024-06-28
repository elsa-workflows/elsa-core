using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Filters;

/// A specification to use when finding workflow execution log records. Only non-null fields will be included in the conditional expression.
public class WorkflowExecutionLogRecordFilter
{
    /// The ID of the workflow execution log record.
    public string? Id { get; set; }
    
    /// The IDs of the workflow execution log records.
    public ICollection<string>? Ids { get; set; }
    
    /// The ID of the workflow instance.
    public string? WorkflowInstanceId { get; set; }
    
    /// The IDs of the workflow instances.
    public ICollection<string>? WorkflowInstanceIds { get; set; }
    
    /// The ID of the parent activity instance.
    public string? ParentActivityInstanceId { get; set; }
    
    /// The ID of the activity.
    public string? ActivityId { get; set; }
    
    /// The IDs of the activities.
    public ICollection<string>? ActivityIds { get; set; }
    
    /// The node ID of the activity.
    public string? ActivityNodeId { get; set; }
    
    /// The node IDs of the activities.
    public ICollection<string>? ActivityNodeIds { get; set; }
    
    /// The activity type to exclude.
    public string? ExcludeActivityType { get; set; }
    
    /// The activity types to exclude.
    public ICollection<string>? ExcludeActivityTypes { get; set; }
    
    /// The name of the event.
    public string? EventName { get; set; }
    
    /// Match all of these event names.
    public ICollection<string>? EventNames { get; set; }
    
    /// Applies the filter to the specified queryable.
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