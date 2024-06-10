using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;

/// <summary>
/// Represents the response for saving a workflow definition.
/// </summary>
public record SaveWorkflowDefinitionResponse(WorkflowDefinition WorkflowDefinition, bool AlreadyPublished, int ConsumingWorkflowCount);