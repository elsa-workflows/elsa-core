using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class SaveWorkflowInstanceHandler : ICommandHandler<SaveWorkflowInstance>
{
    private readonly InMemoryStore<WorkflowInstance> _store;

    public SaveWorkflowInstanceHandler(InMemoryStore<WorkflowInstance> store)
    {
        _store = store;
    }

    public Task<Unit> HandleAsync(SaveWorkflowInstance command, CancellationToken cancellationToken)
    {
        _store.Save(command.WorkflowInstance);
        
        return Task.FromResult(Unit.Instance);
    }
}