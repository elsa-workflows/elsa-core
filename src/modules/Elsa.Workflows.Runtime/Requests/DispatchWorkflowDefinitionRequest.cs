using System.Text.Json.Serialization;
using Elsa.Common.Models;

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
    /// <param name="definitionId">The ID of the workflow definition to dispatch.</param>
    /// <param name="versionOptions">The version options to use when dispatching the workflow definition.</param>
    public DispatchWorkflowDefinitionRequest(string definitionId, VersionOptions versionOptions)
    {
        DefinitionId = definitionId;
        VersionOptions = versionOptions;
    }

    /// <summary>
    /// The ID of the workflow definition to dispatch.
    /// </summary>
    public string DefinitionId { get; init; } = default!;
    
    /// <summary>
    /// The version options to use when dispatching the workflow definition.
    /// </summary>
    public VersionOptions VersionOptions { get; init; }
    
    /// <summary>
    /// Any input to pass to the workflow.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }
    
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
    public string? TriggerActivityId { get; init; }
}