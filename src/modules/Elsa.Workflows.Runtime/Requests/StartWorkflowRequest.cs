using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime;

public class StartWorkflowRequest
{
    /// <summary>
    /// The ID of the workflow definition version to create an instance of.
    /// </summary>
    /// Will be used if <see cref="Workflow"/> is not set.
    public WorkflowDefinitionHandle? WorkflowDefinitionHandle { get; set; }

    /// The workflow to create an instance of.
    /// Will be used if <see cref="WorkflowDefinitionHandle"/> is not set.
    public Workflow? Workflow { get; set; }

    /// The correlation ID of the workflow, if any.
    public string? CorrelationId { get; set; }

    /// The input to the workflow instance, if any.
    public IDictionary<string, object>? Input { get; set; }

    /// Any properties to assign to the workflow instance.
    public IDictionary<string, object>? Properties { get; set; }

    /// The ID of the parent workflow instance, if any.
    public string? ParentId { get; set; }
    
    /// The ID of the activity that triggered the workflow instance, if any.
    public string? TriggerActivityId { get; set; }
    
    /// The handle of the activity to schedule, if any.
    public ActivityHandle? ActivityHandle { get; set; }
}