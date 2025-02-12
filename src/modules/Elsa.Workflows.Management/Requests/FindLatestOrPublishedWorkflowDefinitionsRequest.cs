using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Requests;

/// <summary>
/// A request to find the latest or published workflow definitions.
/// </summary>
public record FindLatestOrPublishedWorkflowDefinitionsRequest(string DefinitionId) : IRequest<ICollection<WorkflowDefinition>>;