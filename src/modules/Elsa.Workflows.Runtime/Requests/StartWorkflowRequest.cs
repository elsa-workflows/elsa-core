using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime;

public class StartWorkflowRequest
{
    /// <summary>
    /// The ID of the workflow definition version to create an instance of.
    /// Will be used if <see cref="Workflow"/> is not set.
    /// </summary>
    public WorkflowDefinitionHandle? WorkflowDefinitionHandle { get; set; }

    /// <summary>
    /// The workflow to create an instance of.
    /// Will be used if <see cref="WorkflowDefinitionHandle"/> is not set.
    /// </summary>
    public Workflow? Workflow { get; set; }

    /// <summary>
    /// The correlation ID of the workflow, if any.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The input to the workflow instance, if any.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

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
}