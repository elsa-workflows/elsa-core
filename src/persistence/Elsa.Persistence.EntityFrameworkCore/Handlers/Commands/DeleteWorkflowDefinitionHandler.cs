using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class DeleteWorkflowDefinitionHandler : ICommandHandler<DeleteWorkflowDefinition, bool>
{
    private readonly IStore<WorkflowDefinition> _store;
    public DeleteWorkflowDefinitionHandler(IStore<WorkflowDefinition> store) => _store = store;
    public async Task<bool> HandleAsync(DeleteWorkflowDefinition command, CancellationToken cancellationToken) => await _store.DeleteWhereAsync(x => x.DefinitionId == command.DefinitionId, cancellationToken) > 0;
}