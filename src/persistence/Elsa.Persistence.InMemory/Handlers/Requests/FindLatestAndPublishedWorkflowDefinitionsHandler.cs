using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;
using Elsa.Persistence.Mappers;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class FindLatestAndPublishedWorkflowDefinitionsHandler : IRequestHandler<FindLatestAndPublishedWorkflows, IEnumerable<Workflow>>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;
    private readonly WorkflowDefinitionMapper _mapper;

    public FindLatestAndPublishedWorkflowDefinitionsHandler(InMemoryStore<WorkflowDefinition> store, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _mapper = mapper;
    }

    public Task<IEnumerable<Workflow>> HandleAsync(FindLatestAndPublishedWorkflows request, CancellationToken cancellationToken)
    {
        var definitions = _store.FindMany(x => x.DefinitionId == request.DefinitionId && (x.IsLatest || x.IsPublished));
        var workflows = _mapper.Map(definitions).ToList().AsEnumerable();
        return Task.FromResult(workflows);
    }
}