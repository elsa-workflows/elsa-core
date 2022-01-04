using Elsa.Mediator.Contracts;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class DeleteWorkflowInstancesHandler : ICommandHandler<DeleteWorkflowInstances, int>
{
    private readonly IStore<WorkflowInstance> _store;
    public DeleteWorkflowInstancesHandler(IStore<WorkflowInstance> store) => _store = store;

    public async Task<int> HandleAsync(DeleteWorkflowInstances command, CancellationToken cancellationToken)
    {
        if (command.DefinitionId != null)
            return await _store.DeleteWhereAsync(x => x.DefinitionId == command.DefinitionId, cancellationToken);

        if (command.InstanceIds != null)
            return await _store.DeleteWhereAsync(x => command.InstanceIds.Contains(x.Id), cancellationToken);

        return 0;
    }
}