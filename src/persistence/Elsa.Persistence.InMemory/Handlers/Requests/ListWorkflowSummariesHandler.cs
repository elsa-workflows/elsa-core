using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;

// ReSharper disable PossibleMultipleEnumeration

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class ListWorkflowSummariesHandler : IRequestHandler<ListWorkflowDefinitionSummaries, Page<WorkflowDefinitionSummary>>
{
    private readonly InMemoryStore<WorkflowDefinition> _store;
    public ListWorkflowSummariesHandler(InMemoryStore<WorkflowDefinition> store) => _store = store;

    public Task<Page<WorkflowDefinitionSummary>> HandleAsync(ListWorkflowDefinitionSummaries request, CancellationToken cancellationToken)
    {
        var query = _store.List().AsQueryable();

        if (request.VersionOptions != null)
            query = query.WithVersion(request.VersionOptions.Value);
        
        var pageArgs = request.PageArgs;

        return query.PaginateAsync(x => WorkflowDefinitionSummary.FromDefinition(x), pageArgs);
    }
}