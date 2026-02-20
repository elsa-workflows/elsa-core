using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Messages;

/// <summary>
/// A request to create and run a new workflow instance.
/// </summary>
[UsedImplicitly]
public class CreateAndRunWorkflowInstanceRequest
{
    /// <summary>
    /// The ID of the workflow definition version to create an instance of.
    /// </summary>
    public WorkflowDefinitionHandle WorkflowDefinitionHandle { get; set; } = null!;

    /// <summary>
    /// The correlation ID of the workflow, if any.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// The name of the workflow instance to be created.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The input to the workflow instance, if any.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// A collection of custom variables used within the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Variables { get; set; }

    /// <summary>
    /// Any properties to assign to the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// The ID of the parent workflow instance, if any.
    /// </summary>
    public string? ParentId { get; set; }
    
    /// <summary>
    /// The ID of the activity that triggered the workflow instance, if any.
    /// </summary>
    public string? TriggerActivityId { get; set; }
    
    /// <summary>
    /// The handle of the activity to schedule, if any.
    /// </summary>
    public ActivityHandle? ActivityHandle { get; set; }

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
    /// The call stack depth of the scheduling activity execution context.
    /// This is used to calculate the call stack depth when the scheduling context is not present
    /// in ActivityExecutionContexts (e.g., for cross-workflow invocations).
    /// Should be set to the depth of the scheduling activity (not depth + 1, as the increment is applied automatically).
    /// </summary>
    public int? SchedulingCallStackDepth { get; set; }
}