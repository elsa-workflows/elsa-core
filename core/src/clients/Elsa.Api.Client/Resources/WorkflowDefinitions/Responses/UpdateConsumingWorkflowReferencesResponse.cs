namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;

/// <summary>
/// Represents a response to a request to update consuming workflow references.
/// </summary>
/// <param name="AffectedWorkflows">The definition IDs of the affected workflows.</param>
public record UpdateConsumingWorkflowReferencesResponse(ICollection<string> AffectedWorkflows);