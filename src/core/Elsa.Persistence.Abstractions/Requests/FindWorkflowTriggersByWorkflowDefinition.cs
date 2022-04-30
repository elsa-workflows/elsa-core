using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Requests;

public record FindWorkflowTriggersByWorkflowDefinition(string WorkflowDefinitionId) : IRequest<ICollection<WorkflowTrigger>>;