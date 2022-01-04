using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class DeleteWorkflowInstancesHandler : ICommandHandler<DeleteWorkflowInstances>
{
    private readonly InMemoryStore<WorkflowInstance> _store;

    public DeleteWorkflowInstancesHandler(InMemoryStore<WorkflowInstance> store)
    {
        _store = store;
    }

    public Task<Unit> HandleAsync(DeleteWorkflowInstances command, CancellationToken cancellationToken)
    {
        if (command.DefinitionId != null)
        {
            _store.DeleteWhere(x => x.DefinitionId == command.DefinitionId);
        }
        else if (command.InstanceIds != null)
        {
            _store.DeleteMany(command.InstanceIds);
        }

        return Task.FromResult(Unit.Instance);
    }
}