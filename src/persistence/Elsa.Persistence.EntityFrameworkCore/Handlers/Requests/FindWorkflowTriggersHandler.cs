using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowTriggersHandler : IRequestHandler<FindWorkflowTriggers, IEnumerable<WorkflowTrigger>>
{
    private readonly IStore<WorkflowTrigger> _store;
    public FindWorkflowTriggersHandler(IStore<WorkflowTrigger> store) => _store = store;

    public async Task<IEnumerable<WorkflowTrigger>> HandleAsync(FindWorkflowTriggers request, CancellationToken cancellationToken) =>
        await _store.FindManyAsync(x => x.Name == request.Name && x.Hash == request.Hash, cancellationToken);
}