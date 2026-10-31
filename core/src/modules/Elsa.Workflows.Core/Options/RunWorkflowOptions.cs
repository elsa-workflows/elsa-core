using Elsa.Workflows.Models;

namespace Elsa.Workflows.Options;

/// <summary>
/// Provides options for running a workflow.
/// </summary>
public class RunWorkflowOptions
{
    public string? WorkflowInstanceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Variables { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? TriggerActivityId { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }

    /// <summary>
    /// The ID of the activity execution context that scheduled this workflow execution (for cross-workflow call stack tracking).
    /// This is set when a parent workflow invokes this workflow via ExecuteWorkflow or DispatchWorkflow.
    /// </summary>
    public string? SchedulingActivityExecutionId { get; set; }

    /// <summary>
    /// The workflow instance ID of the parent workflow that scheduled this workflow execution.
    /// This is set when crossing workflow boundaries.
    /// </summary>
    public string? SchedulingWorkflowInstanceId { get; set; }

    /// <summary>
    /// The call stack depth of the scheduling activity execution context.
    /// This is used to calculate the call stack depth when the scheduling context is not present
    /// in ActivityExecutionContexts (e.g., for cross-workflow invocations).
    /// Should be set to the depth of the scheduling activity (not depth + 1, as the increment is applied automatically).
    /// </summary>
    public int? SchedulingCallStackDepth { get; set; }
}