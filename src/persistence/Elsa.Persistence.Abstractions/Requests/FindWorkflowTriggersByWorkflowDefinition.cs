using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Requests;

public record FindWorkflowTriggersByWorkflowDefinition(string WorkflowDefinitionId) : IRequest<ICollection<WorkflowTrigger>>;