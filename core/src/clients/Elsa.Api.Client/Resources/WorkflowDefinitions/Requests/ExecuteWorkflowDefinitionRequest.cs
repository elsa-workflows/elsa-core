using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;

/// <summary>
/// A request to execute a workflow definition.
/// </summary>
public class ExecuteWorkflowDefinitionRequest
{
    /// <summary>
    /// An optional correlation ID to associate with the workflow instance.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// An optional activity ID to trigger.
    /// </summary>
    public string? TriggerActivityId { get; set; }
    
    /// <summary>
    /// An optional version to execute.
    /// </summary>
    public VersionOptions? VersionOptions { get; set; }
    
    /// <summary>
    /// Optional input to pass to the workflow instance.
    /// </summary>
    public object? Input { get; set; }
}