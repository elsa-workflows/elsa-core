using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowTriggersByWorkflowDefinitionHandler : IRequestHandler<FindWorkflowTriggersByWorkflowDefinition, ICollection<WorkflowTrigger>>
{
    private readonly IStore<WorkflowTrigger> _store;
    public FindWorkflowTriggersByWorkflowDefinitionHandler(IStore<WorkflowTrigger> store) => _store = store;

    public async Task<ICollection<WorkflowTrigger>> HandleAsync(FindWorkflowTriggersByWorkflowDefinition request, CancellationToken cancellationToken)
    {
        var triggers = await _store.QueryAsync(query => query.Where(x => x.WorkflowDefinitionId == request.WorkflowDefinitionId), cancellationToken);
        return triggers.ToList();
    }
}