using System.Collections.Generic;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Commands;

public record ReplaceWorkflowTriggers(string WorkflowId, IEnumerable<WorkflowTrigger> WorkflowTriggers) : ICommand;