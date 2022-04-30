using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindWorkflowTriggersByWorkflowDefinitionHandler : IRequestHandler<FindWorkflowTriggersByWorkflowDefinition, ICollection<WorkflowTrigger>>
{
    private readonly InMemoryStore<WorkflowTrigger> _store;
    public FindWorkflowTriggersByWorkflowDefinitionHandler(InMemoryStore<WorkflowTrigger> store) => _store = store;

    public Task<ICollection<WorkflowTrigger>> HandleAsync(FindWorkflowTriggersByWorkflowDefinition request, CancellationToken cancellationToken)
    {
        var triggers = _store.Query(query => query.Where(x => x.WorkflowDefinitionId == request.WorkflowDefinitionId));

        return Task.FromResult<ICollection<WorkflowTrigger>>(triggers.ToList());
    }
}