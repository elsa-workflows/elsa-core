using System.Text.Json.Serialization;

namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// A request to dispatch a workflow definition for execution.
/// </summary>
public class DispatchWorkflowDefinitionRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchWorkflowDefinitionRequest"/> class.
    /// </summary>
    [JsonConstructor]
    public DispatchWorkflowDefinitionRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchWorkflowDefinitionRequest"/> class.
    /// </summary>
    /// <param name="definitionVersionId">The ID of the workflow definition version to dispatch.</param>
    public DispatchWorkflowDefinitionRequest(string definitionVersionId)
    {
        DefinitionVersionId = definitionVersionId;
    }

    /// <summary>
    /// The ID of the workflow definition version to dispatch.
    /// </summary>
    public string DefinitionVersionId { get; set; } = default!;

    /// <summary>
    /// The ID of the parent workflow instance.
    /// </summary>
    public string? ParentWorkflowInstanceId { get; set; }

    /// <summary>
    /// Any input to pass to the workflow.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// Any properties to attach to the workflow.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }

    /// <summary>
    /// The correlation ID to use when dispatching the workflow.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The ID to use when creating an instance of the workflow to dispatch.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// The ID of the activity that triggered the workflow.
    /// </summary>
    public string? TriggerActivityId { get; set; }
}