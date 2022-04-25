using Elsa.Mediator.Services;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;

public class DeleteWorkflowInstancesHandler : ICommandHandler<DeleteWorkflowInstances, int>, ICommandHandler<DeleteWorkflowInstance, int>
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

    public async Task<int> HandleAsync(DeleteWorkflowInstance command, CancellationToken cancellationToken)
    {
        if (command.InstanceId != null)
            return await _store.DeleteWhereAsync(x => x.Id == command.InstanceId, cancellationToken);

        return 0;
    }
}