namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;

/// <summary>
/// The response from a request to publish workflow definitions.
/// </summary>
public record BulkRetractWorkflowDefinitionsResponse(
    ICollection<string> Retracted, 
    ICollection<string> AlreadyRetracted, 
    ICollection<string> NotFound);