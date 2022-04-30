using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class ReplaceWorkflowTriggersHandler : ICommandHandler<ReplaceWorkflowTriggers>
{
    private readonly IStore<WorkflowTrigger> _store;
    public ReplaceWorkflowTriggersHandler(IStore<WorkflowTrigger> store) => _store = store;

    public async Task<Unit> HandleAsync(ReplaceWorkflowTriggers command, CancellationToken cancellationToken)
    {
        await _store.DeleteManyAsync(command.Removed, cancellationToken);
        await _store.SaveManyAsync(command.Added, cancellationToken);

        return Unit.Instance;
    }
}