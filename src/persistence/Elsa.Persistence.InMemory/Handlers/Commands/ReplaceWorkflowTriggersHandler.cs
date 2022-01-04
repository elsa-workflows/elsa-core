using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class ReplaceWorkflowTriggersHandler : ICommandHandler<ReplaceWorkflowTriggers>
{
    private readonly InMemoryStore<WorkflowTrigger> _store;

    public ReplaceWorkflowTriggersHandler(InMemoryStore<WorkflowTrigger> store)
    {
        _store = store;
    }

    public Task<Unit> HandleAsync(ReplaceWorkflowTriggers command, CancellationToken cancellationToken)
    {
        _store.DeleteWhere(x => x.WorkflowDefinitionId == command.WorkflowId);
        _store.SaveMany(command.WorkflowTriggers);
        
        return Task.FromResult(Unit.Instance);
    }
}