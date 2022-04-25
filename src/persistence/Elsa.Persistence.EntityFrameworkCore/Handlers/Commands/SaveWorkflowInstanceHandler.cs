using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;

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