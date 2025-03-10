using Elsa.Workflows.Models;

namespace Elsa.Scheduling;

public class ScheduleNewWorkflowInstanceRequest
{
    /// <summary>
    /// The ID of the workflow definition version to create an instance of.
    /// </summary>
    public WorkflowDefinitionHandle WorkflowDefinitionHandle { get; set; } = default!;

    /// <summary>
    /// The correlation ID of the workflow, if any.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The input to the workflow instance, if any.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// A collection of variables to pass to the workflow instance during scheduling.
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
}