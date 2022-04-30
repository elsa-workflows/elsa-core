using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindLatestAndPublishedWorkflowDefinitionsHandler : IRequestHandler<FindLatestAndPublishedWorkflows, IEnumerable<WorkflowDefinition>>
{
    private readonly IStore<WorkflowDefinition> _store;
    public FindLatestAndPublishedWorkflowDefinitionsHandler(IStore<WorkflowDefinition> store) => _store = store;

    public async Task<IEnumerable<WorkflowDefinition>> HandleAsync(FindLatestAndPublishedWorkflows request, CancellationToken cancellationToken) =>
        await _store.FindManyAsync(x => x.DefinitionId == request.DefinitionId && (x.IsLatest || x.IsPublished), cancellationToken);
}