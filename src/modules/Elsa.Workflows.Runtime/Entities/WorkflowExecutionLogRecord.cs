using Elsa.Common.Entities;

namespace Elsa.Workflows.Runtime.Entities;

/// <summary>
/// Represents a workflow execution log entry.
/// </summary>
public class WorkflowExecutionLogRecord : Entity
{
    /// <summary>
    /// The ID of the workflow definition.
    /// </summary>
    public string WorkflowDefinitionId { get; set; } = default!;
    
    /// <summary>
    /// The version ID of the workflow definition.
    /// </summary>
    public string WorkflowDefinitionVersionId { get; set; } = default!;
    
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;
    
    /// <summary>
    /// The version of the workflow definition.
    /// </summary>
    public int WorkflowVersion { get; init; }
    
    /// <summary>
    /// The ID of the activity instance.
    /// </summary>
    public string ActivityInstanceId { get; set; } = default!;
    
    /// <summary>
    /// The ID of the parent activity instance.
    /// </summary>
    public string? ParentActivityInstanceId { get; set; }
    
    /// <summary>
    /// The ID of the activity.
    /// </summary>
    public string ActivityId { get; set; } = default!;
    
    /// <summary>
    /// The type of the activity.
    /// </summary>
    public string ActivityType { get; set; } = default!;

    /// <summary>
    /// The version of the activity type.
    /// </summary>
    public int ActivityTypeVersion { get; set; }

    /// <summary>
    /// The name of the activity.
    /// </summary>
    public string? ActivityName { get; set; }

    /// <summary>
    /// The unique ID of the node within the workflow graph.
    /// </summary>
    public string NodeId { get; set; } = default!;

    /// <summary>
    /// The time stamp of the log entry.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// A local sequence number of the log entry within a batch of log entries. This is used to order log entries whose timestamps are (close to) identical.
    /// </summary>
    public long Sequence { get; set; }
    
    /// <summary>
    /// The name of the event.
    /// </summary>
    public string? EventName { get; set; }
    
    /// <summary>
    /// The message.
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// The source of the event.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The state of the activity at the time of the log entry.
    /// </summary>
    public IDictionary<string, object>? ActivityState { get; set; }
    
    /// <summary>
    /// Any additional payload associated with the log entry.
    /// </summary>
    public object? Payload { get; set; }
}