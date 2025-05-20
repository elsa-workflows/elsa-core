namespace Elsa.Workflows.Runtime.Responses;

/// <summary>
/// Represents a response to a request to refresh workflow definitions.
/// </summary>
public record RefreshWorkflowDefinitionsResponse(ICollection<string> Refreshed, ICollection<string> NotFound);