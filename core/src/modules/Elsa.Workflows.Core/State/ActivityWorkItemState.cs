using Elsa.Workflows.Memory;

namespace Elsa.Workflows.State;

/// <summary>
/// A serialized representation of an activity work item.
/// </summary>
public class ActivityWorkItemState
{
    /// <summary>
    /// The ID of the activity to be executed.
    /// </summary>
    public string ActivityNodeId { get; set; } = default!;
    
    /// <summary>
    /// The ID of the parent activity execution context, if any.
    /// </summary>
    public string? OwnerContextId { get; set; }
    
    /// <summary>
    /// A tag that can be used to identify the work item.
    /// </summary>
    public object? Tag { get; set; }
    
    /// <summary>
    /// A set of variables to be created as part of the activity execution context, if any.
    /// </summary>
    public ICollection<Variable>? Variables { get; set; }
    
    /// <summary>
    /// The ID of an existing activity execution context to schedule, if any.
    /// </summary>
    public string? ExistingActivityExecutionContextId { get; set; }

    /// <summary>
    /// Optional input to pass to the activity.
    /// </summary>
    public IDictionary<string, object> Input { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// The ID of the activity execution context that scheduled this work item.
    /// </summary>
    public string? SchedulingActivityExecutionId { get; set; }

    /// <summary>
    /// The workflow instance ID of the workflow that scheduled this work item, if it crossed a workflow boundary.
    /// </summary>
    public string? SchedulingWorkflowInstanceId { get; set; }

    /// <summary>
    /// The call stack depth of the activity execution context that scheduled this work item.
    /// </summary>
    public int? SchedulingCallStackDepth { get; set; }
}
