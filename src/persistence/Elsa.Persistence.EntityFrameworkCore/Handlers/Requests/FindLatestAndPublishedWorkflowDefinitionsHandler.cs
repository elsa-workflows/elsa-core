using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.Mappers;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindLatestAndPublishedWorkflowDefinitionsHandler : IRequestHandler<FindLatestAndPublishedWorkflows, IEnumerable<Workflow>>
{
    private readonly IStore<WorkflowDefinition> _store;
    private readonly WorkflowDefinitionMapper _mapper;

    public FindLatestAndPublishedWorkflowDefinitionsHandler(IStore<WorkflowDefinition> store, WorkflowDefinitionMapper mapper)
    {
        _store = store;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Workflow>> HandleAsync(FindLatestAndPublishedWorkflows request, CancellationToken cancellationToken)
    {
        var definitions = await _store.FindManyAsync(x => x.DefinitionId == request.DefinitionId && (x.IsLatest || x.IsPublished), cancellationToken);
        return _mapper.Map(definitions).ToList();
    }
}