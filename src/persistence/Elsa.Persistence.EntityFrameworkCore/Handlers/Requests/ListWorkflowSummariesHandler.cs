using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class ListWorkflowSummariesHandler : IRequestHandler<ListWorkflowSummaries, PagedList<WorkflowSummary>>
{
    private readonly IStore<WorkflowDefinition> _store;
    public ListWorkflowSummariesHandler(IStore<WorkflowDefinition> store) => _store = store;

    public async Task<PagedList<WorkflowSummary>> HandleAsync(ListWorkflowSummaries request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.WorkflowDefinitions;
        var query = set.AsQueryable();
        
        if (request.VersionOptions != null)
            query = query.WithVersion(request.VersionOptions.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        if (request.Skip != null)
            query = query.Skip(request.Skip.Value);

        if (request.Take != null)
            query = query.Take(request.Take.Value);

        var summaries = query.OrderBy(x => x.Name).Select(x => WorkflowSummary.FromDefinition(x)).ToList();
        return new PagedList<WorkflowSummary>(summaries, request.Take ?? totalCount, totalCount);
    }
}