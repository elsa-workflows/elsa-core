using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;

namespace Elsa.Persistence.InMemory.Handlers.Commands;

public class SaveWorkflowDefinitionHandler : ICommandHandler<SaveWorkflowDefinition>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;
    public SaveWorkflowDefinitionHandler(InMemoryStore<WorkflowDefinition> store) => _store = store;

    public Task<Unit> HandleAsync(SaveWorkflowDefinition command, CancellationToken cancellationToken)
    {
        _store.Save(command.WorkflowDefinition);
        
        return Task.FromResult(Unit.Instance);
    }
}