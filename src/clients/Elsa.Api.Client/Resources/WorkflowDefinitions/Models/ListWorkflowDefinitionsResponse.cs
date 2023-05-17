namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents a response from listing workflow definitions.
/// </summary>
/// <param name="Items">The workflow definitions.</param>
/// <param name="TotalCount">The total number of workflow definitions.</param>
public record ListWorkflowDefinitionsResponse(ICollection<WorkflowDefinitionSummary> Items, int TotalCount);