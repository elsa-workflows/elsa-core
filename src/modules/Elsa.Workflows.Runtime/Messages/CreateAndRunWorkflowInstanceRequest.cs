using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Messages;

/// <summary>
/// A request to create and run a new workflow instance.
/// </summary>
[UsedImplicitly]
public class CreateAndRunWorkflowInstanceRequest
{
    /// The ID of the workflow definition version to create an instance of.
    public WorkflowDefinitionHandle WorkflowDefinitionHandle { get; set; } = default!;

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