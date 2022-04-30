using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class FindManyWorkflowDefinitionsHandler : IRequestHandler<FindManyWorkflowDefinitions, IEnumerable<WorkflowDefinitionSummary>>
{
    private readonly IStore<WorkflowDefinition> _store;
    public FindManyWorkflowDefinitionsHandler(IStore<WorkflowDefinition> store) => _store = store;

    public async Task<IEnumerable<WorkflowDefinitionSummary>> HandleAsync(FindManyWorkflowDefinitions request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var query = set.AsQueryable();

        if (request.VersionOptions != null)
            query = query.WithVersion(request.VersionOptions.Value);
        
        query = query.Where(x => request.DefinitionIds.Contains(x.DefinitionId));

        return query.OrderBy(x => x.Name).Select(x => WorkflowDefinitionSummary.FromDefinition(x)).ToList();
    }
}