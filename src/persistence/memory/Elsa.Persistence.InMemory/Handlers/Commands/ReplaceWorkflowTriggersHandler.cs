using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;

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
        _store.DeleteMany(command.Removed);
        _store.SaveMany(command.Added);

        return Task.FromResult(Unit.Instance);
    }
}