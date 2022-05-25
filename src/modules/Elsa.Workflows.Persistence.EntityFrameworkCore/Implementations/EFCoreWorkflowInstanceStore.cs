using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.Common.Models;
using Elsa.Persistence.EntityFrameworkCore.Common.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Implementations;

public class EFCoreWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly IStore<WorkflowsDbContext, WorkflowInstance> _store;
    private readonly IStore<WorkflowsDbContext, WorkflowBookmark> _bookmarkStore;
    private readonly IStore<WorkflowsDbContext, WorkflowExecutionLogRecord> _executionLogRecordStore;

    public EFCoreWorkflowInstanceStore(
        IStore<WorkflowsDbContext, WorkflowInstance> store,
        IStore<WorkflowsDbContext, WorkflowBookmark> bookmarkStore,
        IStore<WorkflowsDbContext, WorkflowExecutionLogRecord> executionLogRecordStore)
    {
        _store = store;
        _bookmarkStore = bookmarkStore;
        _executionLogRecordStore = executionLogRecordStore;
    }

    public async Task<WorkflowInstance?> FindByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _store.FindAsync(x => x.Id == id, cancellationToken);

    public async Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default) =>
        await _store.SaveAsync(record, cancellationToken);

    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default) =>
        await _store.SaveManyAsync(records, cancellationToken);

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        await _bookmarkStore.DeleteWhereAsync(x => idList.Contains(x.WorkflowDefinitionId), cancellationToken);
        await _executionLogRecordStore.DeleteWhereAsync(x => idList.Contains(x.WorkflowDefinitionId), cancellationToken);
        return await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }

    public async Task DeleteManyByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        await _bookmarkStore.DeleteWhereAsync(x => x.WorkflowDefinitionId == definitionId, cancellationToken);
        await _executionLogRecordStore.DeleteWhereAsync(x => x.WorkflowDefinitionId == definitionId, cancellationToken);
        await _store.DeleteWhereAsync(x => x.DefinitionId == definitionId, cancellationToken);
    }

    public async Task<Page<WorkflowInstanceSummary>> FindManyAsync(FindWorkflowInstancesArgs args, CancellationToken cancellationToken = default)
    {
        var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var query = dbContext.WorkflowInstances.AsQueryable();
        var (searchTerm, definitionId, version, correlationId, workflowStatus, workflowSubStatus, pageArgs, orderBy, orderDirection) = args;

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
                      || instance.CorrelationId!.Contains(searchTerm)
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