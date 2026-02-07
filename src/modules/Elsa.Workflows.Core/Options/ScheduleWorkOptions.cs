using Elsa.Workflows.Memory;

namespace Elsa.Workflows.Options;

/// <summary>
/// Represents options for scheduling a work item.
/// </summary>
public class ScheduleWorkOptions
{
    /// <summary>The callback to invoke when the work item has completed.</summary>
    public ActivityCompletionCallback? CompletionCallback { get; set; }

    /// <summary>A tag that can be used to identify the work item.</summary>
    public object? Tag { get; set; }

    /// <summary>A collection of variables to declare in the activity execution context that will be created for this work item.</summary>
    public ICollection<Variable>? Variables { get; set; }

    /// <summary>
    /// An existing activity execution context to use instead of creating a new one.
    /// </summary>
    public ActivityExecutionContext? ExistingActivityExecutionContext { get; set; }
    
    /// <summary>
    /// A flag indicating whether the work item should be scheduled even if a work item with the same tag already exists.
    /// </summary>
    public bool PreventDuplicateScheduling { get; set; }
    
    /// <summary>
    /// Input to send to the workflow.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// The ID of the activity execution context that scheduled this work item.
    /// This represents the temporal/execution predecessor that directly triggered execution of this activity,
    /// distinct from the structural parent (<see cref="ExistingActivityExecutionContext"/> or Owner).
    /// </summary>
    public string? SchedulingActivityExecutionId { get; set; }

    /// <summary>
    /// The workflow instance ID of the workflow that scheduled this work item.
    /// This is set when crossing workflow boundaries (e.g., via ExecuteWorkflow or DispatchWorkflow).
    /// For activities within the same workflow instance, this will be null.
    /// </summary>
    public string? SchedulingWorkflowInstanceId { get; set; }
}