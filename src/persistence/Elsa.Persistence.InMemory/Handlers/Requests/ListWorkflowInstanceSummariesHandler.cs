using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Services;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;

// ReSharper disable PossibleMultipleEnumeration

namespace Elsa.Persistence.InMemory.Handlers.Requests;

public class ListWorkflowInstanceSummariesHandler : IRequestHandler<ListWorkflowInstanceSummaries, PagedList<WorkflowInstanceSummary>>
{
    private readonly InMemoryStore<WorkflowInstance> _store;
    public ListWorkflowInstanceSummariesHandler(InMemoryStore<WorkflowInstance> store) => _store = store;

    public Task<PagedList<WorkflowInstanceSummary>> HandleAsync(ListWorkflowInstanceSummaries request, CancellationToken cancellationToken)
    {
        var query = _store.List();
        var (searchTerm, definitionId, version, correlationId, workflowStatus, orderBy, orderDirection, skip, take) = request;

        if (!string.IsNullOrWhiteSpace(definitionId))
            query = query.Where(x => x.DefinitionId == definitionId);

        if (version != null)
            query = query.Where(x => x.Version == version);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId);

        if (workflowStatus != null)
            query = query.Where(x => x.WorkflowStatus == workflowStatus);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            const StringComparison comparison = StringComparison.InvariantCultureIgnoreCase;

            query =
                from instance in query
                where instance.Name?.Contains(searchTerm, comparison) == true
                      || instance.Id.Contains(searchTerm, comparison)
                      || instance.DefinitionId.Contains(searchTerm, comparison)
                      || instance.CorrelationId.Contains(searchTerm, comparison)
                select instance;
        }

        query = orderBy switch
        {
            OrderBy.Finished => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.FinishedAt) : query.OrderByDescending(x => x.FinishedAt),
            OrderBy.LastExecuted => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.LastExecutedAt) : query.OrderByDescending(x => x.LastExecutedAt),
            OrderBy.Created => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt),
            _ => query
        };

        var totalCount = query.Count();
        var entities = query.Skip(skip).Take(take).ToList();
        var summaries = entities.Select(WorkflowInstanceSummary.FromInstance).ToList();
        var pagedList = new PagedList<WorkflowInstanceSummary>(summaries, take, totalCount);
        return Task.FromResult(pagedList);
    }
}