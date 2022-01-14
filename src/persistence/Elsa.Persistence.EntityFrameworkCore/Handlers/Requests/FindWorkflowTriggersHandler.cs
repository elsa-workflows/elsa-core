using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowTriggersHandler : IRequestHandler<FindWorkflowTriggers, IEnumerable<WorkflowTrigger>>
{
    private readonly IStore<WorkflowTrigger> _store;
    public FindWorkflowTriggersHandler(IStore<WorkflowTrigger> store) => _store = store;

    public async Task<IEnumerable<WorkflowTrigger>> HandleAsync(FindWorkflowTriggers request, CancellationToken cancellationToken)
    {
        return await _store.QueryAsync(query =>
        {
            query = query.Where(x => x.Name == request.Name);

            if (request.Hash != null)
                query = query.Where(x => x.Hash == request.Hash);

            return query;
        }, cancellationToken);
    }
}