namespace Elsa.Http.OpenApi.Models;

/// <summary>
/// Represents an HTTP endpoint definition extracted from a workflow.
/// </summary>
/// <param name="Path">The HTTP path of the endpoint.</param>
/// <param name="Method">The HTTP method (GET, POST, etc.).</param>
/// <param name="Summary">Optional summary description of the endpoint.</param>
/// <param name="WorkflowDefinitionId">The workflow definition ID that contains this endpoint.</param>
/// <param name="WorkflowDefinitionName">The name of the workflow definition.</param>
/// <param name="WorkflowVersion">The version of the workflow definition.</param>
public record EndpointDefinition(
    string Path, 
    string Method, 
    string? Summary = null,
    string? WorkflowDefinitionId = null,
    string? WorkflowDefinitionName = null,
    int? WorkflowVersion = null);
