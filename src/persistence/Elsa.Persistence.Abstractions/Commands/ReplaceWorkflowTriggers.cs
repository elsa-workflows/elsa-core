using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Commands;

public record ReplaceWorkflowTriggers(ICollection<WorkflowTrigger> WorkflowTriggers) : ICommand;