using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindLatestAndPublishedWorkflowDefinitionsHandler : IRequestHandler<FindLatestAndPublishedWorkflows, IEnumerable<WorkflowDefinition>>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;

    public FindLatestAndPublishedWorkflowDefinitionsHandler(InMemoryStore<WorkflowDefinition> store)
    {
        _store = store;
    }

    public Task<IEnumerable<WorkflowDefinition>> HandleAsync(FindLatestAndPublishedWorkflows request, CancellationToken cancellationToken)
    {
        var definitions = _store.FindMany(x => x.DefinitionId == request.DefinitionId && (x.IsLatest || x.IsPublished));
        return Task.FromResult(definitions);
    }
}