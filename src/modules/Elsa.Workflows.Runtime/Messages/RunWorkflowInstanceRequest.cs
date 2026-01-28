using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Messages;

/// <summary>
/// A request to run a workflow instance.
/// </summary>
[UsedImplicitly]
public class RunWorkflowInstanceRequest
{
    /// <summary>
    /// The ID of the activity that triggered the workflow instance, if any.
    /// </summary>
    public string? TriggerActivityId { get; set; }
    
    /// <summary>
    /// The ID of the bookmark that triggered the workflow instance, if any.
    /// </summary>
    public string? BookmarkId { get; set; }
    
    /// <summary>
    /// The handle of the activity to schedule, if any.
    /// </summary>
    public ActivityHandle? ActivityHandle { get; set; }
    
    /// <summary>
    /// Any additional properties to associate with the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }
    
    /// <summary>
    /// The input to the workflow instance, if any.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// A collection of variables to be used during the execution of a workflow instance.
    /// </summary>
    public IDictionary<string, object>? Variables { get; set; }

    /// <summary>
    /// When set to <c>true</c>, include workflow output in the response.
    /// </summary>
    public bool IncludeWorkflowOutput { get; set; }

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
    /// Represents an empty <see cref="RunWorkflowInstanceRequest"/> object used as a default value.
    /// </summary>
    public static RunWorkflowInstanceRequest Empty => new();
}