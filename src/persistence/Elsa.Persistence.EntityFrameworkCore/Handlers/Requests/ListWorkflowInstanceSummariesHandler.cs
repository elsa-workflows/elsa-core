using Elsa.Mediator.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;

namespace Elsa.Persistence.EntityFrameworkCore.Handlers.Requests;

public class ListWorkflowInstanceSummariesHandler : IRequestHandler<ListWorkflowInstanceSummaries, Page<WorkflowInstanceSummary>>
{
    private readonly IStore<WorkflowInstance> _store;
    public ListWorkflowInstanceSummariesHandler(IStore<WorkflowInstance> store) => _store = store;

    public async Task<Page<WorkflowInstanceSummary>> HandleAsync(ListWorkflowInstanceSummaries request, CancellationToken cancellationToken)
    {
        var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var query = dbContext.WorkflowInstances.AsQueryable();
        var (searchTerm, definitionId, version, correlationId, workflowStatus, workflowSubStatus, pageArgs, orderBy, orderDirection) = request;

        if (!string.IsNullOrWhiteSpace(definitionId))
            query = query.Where(x => x.DefinitionId == definitionId);

        if (version != null)
            query = query.Where(x => x.Version == version);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId);

        if (workflowStatus != null)
            query = query.Where(x => x.Status == workflowStatus);

        if (workflowSubStatus != null)
            query = query.Where(x => x.SubStatus == workflowSubStatus);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query =
                from instance in query
                where instance.Name!.Contains(searchTerm)
                      || instance.Id.Contains(searchTerm)
                      || instance.DefinitionId.Contains(searchTerm)
                      || instance.CorrelationId.Contains(searchTerm)
                select instance;
        }

        query = orderBy switch
        {
            OrderBy.Finished => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.FinishedAt) : query.OrderByDescending(x => x.FinishedAt),
            OrderBy.LastExecuted => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.LastExecutedAt) : query.OrderByDescending(x => x.LastExecutedAt),
            OrderBy.Created => orderDirection == OrderDirection.Ascending ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt),
            _ => query
        };

        return await query.PaginateAsync(x => WorkflowInstanceSummary.FromInstance(x), pageArgs);
    }
}