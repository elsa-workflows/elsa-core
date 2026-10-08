namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;

/// <summary>
/// A response containing the IDs of workflow definitions that consume a specified workflow definition.
/// </summary>
/// <param name="ConsumingWorkflowDefinitionIds">The IDs of consuming workflow definitions.</param>
public record GetConsumingWorkflowDefinitionsResponse(ICollection<string> ConsumingWorkflowDefinitionIds);

