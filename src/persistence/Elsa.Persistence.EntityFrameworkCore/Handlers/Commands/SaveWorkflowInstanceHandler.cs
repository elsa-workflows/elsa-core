using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class SaveWorkflowInstanceHandler : ICommandHandler<SaveWorkflowInstance>
{
    private readonly IStore<WorkflowInstance> _store;
    public SaveWorkflowInstanceHandler(IStore<WorkflowInstance> store) => _store = store;

    public async Task<Unit> HandleAsync(SaveWorkflowInstance command, CancellationToken cancellationToken)
    {
        await _store.SaveAsync(command.WorkflowInstance, cancellationToken);

        return Unit.Instance;
    }
}