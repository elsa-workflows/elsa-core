using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindWorkflowTriggersHandler : IRequestHandler<FindWorkflowTriggers, IEnumerable<WorkflowTrigger>>
{
    private readonly InMemoryStore<WorkflowTrigger> _store;
    public FindWorkflowTriggersHandler(InMemoryStore<WorkflowTrigger> store) => _store = store;

    public Task<IEnumerable<WorkflowTrigger>> HandleAsync(FindWorkflowTriggers request, CancellationToken cancellationToken)
    {
        var triggers = _store.Query(query =>
        {
            query = query.Where(x => x.Name == request.Name);

            if (request.Hash != null)
                query = query.Where(x => x.Hash == request.Hash);

            return query;
        });

        return Task.FromResult(triggers);
    }
}