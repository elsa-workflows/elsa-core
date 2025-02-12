using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Requests;

/// <summary>
/// A request to find the last version of a workflow definition.
/// </summary>
/// <param name="DefinitionId">The ID of the workflow definition.</param>
public record FindLastVersionOfWorkflowDefinitionRequest(string DefinitionId) : IRequest<WorkflowDefinition?>;