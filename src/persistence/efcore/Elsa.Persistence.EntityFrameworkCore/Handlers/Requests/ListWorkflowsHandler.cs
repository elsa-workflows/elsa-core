using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class ListWorkflowsHandler : IRequestHandler<ListWorkflowDefinitions, Page<WorkflowDefinition>>
{
    private readonly IStore<WorkflowDefinition> _store;

    public ListWorkflowsHandler(IStore<WorkflowDefinition> store)
    {
        _store = store;
    }

    public async Task<Page<WorkflowDefinition>> HandleAsync(ListWorkflowDefinitions request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var query = set.AsQueryable();
        
        if (request.VersionOptions != null)
            query = query.WithVersion(request.VersionOptions.Value);

        query = query.OrderBy(x => x.Name);
        return await query.PaginateAsync(request.PageArgs);
    }
}