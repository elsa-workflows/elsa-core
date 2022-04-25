using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindWorkflowTriggersByNameHandler : IRequestHandler<FindWorkflowTriggersByName, ICollection<WorkflowTrigger>>
{
    private readonly IStore<WorkflowTrigger> _store;
    public FindWorkflowTriggersByNameHandler(IStore<WorkflowTrigger> store) => _store = store;

    public async Task<ICollection<WorkflowTrigger>> HandleAsync(FindWorkflowTriggersByName request, CancellationToken cancellationToken)
    {
        var triggers = await _store.QueryAsync(query =>
        {
            query = query.Where(x => x.Name == request.Name);

            if (request.Hash != null)
                query = query.Where(x => x.Hash == request.Hash);

            return query;
        }, cancellationToken);

        return triggers.ToList();
    }
}