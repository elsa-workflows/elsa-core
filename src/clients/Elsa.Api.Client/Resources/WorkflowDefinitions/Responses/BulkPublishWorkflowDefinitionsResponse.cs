namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;

/// <summary>
/// The response from a request to publish workflow definitions.
/// </summary>
public record BulkPublishWorkflowDefinitionsResponse(
    ICollection<string> Published, 
    ICollection<string> AlreadyPublished, 
    ICollection<string> NotFound);