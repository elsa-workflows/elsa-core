using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Commands;

public record ReplaceWorkflowTriggers(Workflow Workflow, ICollection<WorkflowTrigger> Removed, ICollection<WorkflowTrigger> Added) : ICommand;