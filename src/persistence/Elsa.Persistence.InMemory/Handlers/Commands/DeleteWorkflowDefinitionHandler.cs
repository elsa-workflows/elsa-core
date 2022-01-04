using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class DeleteWorkflowDefinitionHandler : ICommandHandler<DeleteWorkflowDefinition>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;

    public DeleteWorkflowDefinitionHandler(InMemoryStore<WorkflowDefinition> store)
    {
        _store = store;
    }

    public Task<Unit> HandleAsync(DeleteWorkflowDefinition command, CancellationToken cancellationToken)
    {
        _store.Delete(command.DefinitionId);
        
        return Task.FromResult(Unit.Instance);
    }
}