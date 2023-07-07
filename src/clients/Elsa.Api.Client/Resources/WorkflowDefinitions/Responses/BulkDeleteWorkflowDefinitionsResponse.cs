namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;

/// <summary>
/// The response from a request to delete workflow definitions.
/// </summary>
/// <param name="Deleted">The number of workflow definitions deleted.</param>
public record BulkDeleteWorkflowDefinitionsResponse(long Deleted);