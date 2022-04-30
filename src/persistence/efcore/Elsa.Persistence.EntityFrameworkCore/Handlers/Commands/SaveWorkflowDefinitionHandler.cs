using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class SaveWorkflowDefinitionHandler : ICommandHandler<SaveWorkflowDefinition>
{
    private readonly IStore<WorkflowDefinition> _store;
    public SaveWorkflowDefinitionHandler(IStore<WorkflowDefinition> store) => _store = store;

    public async Task<Unit> HandleAsync(SaveWorkflowDefinition command, CancellationToken cancellationToken)
    {
        await _store.SaveAsync(command.WorkflowDefinition, cancellationToken);
        
        return Unit.Instance;
    }
}