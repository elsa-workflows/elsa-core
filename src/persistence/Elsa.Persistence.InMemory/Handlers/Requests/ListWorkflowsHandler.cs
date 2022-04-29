using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class ListWorkflowsHandler : IRequestHandler<ListWorkflowDefinitions, Page<WorkflowDefinition>>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;

    public ListWorkflowsHandler(InMemoryStore<WorkflowDefinition> store)
    {
        _store = store;
    }

    public Task<Page<WorkflowDefinition>> HandleAsync(ListWorkflowDefinitions request, CancellationToken cancellationToken)
    {
        var query = _store.List().AsQueryable();

        if (request.VersionOptions != null)
            query = query.WithVersion(request.VersionOptions.Value);

        var totalCount = query.Count();
        var pageArgs = request.PageArgs;

        if (pageArgs?.Offset != null) query = query.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) query = query.Take(pageArgs.Limit.Value);

        var entities = query.ToList();
        var page = Page.Of(entities, totalCount);
        return Task.FromResult(page);
    }
}