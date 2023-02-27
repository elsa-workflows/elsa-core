using Elsa.Common.Entities;
using Elsa.Common.Implementations;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

/// <summary>
/// A non-persistent memory store for saving and loading <see cref="WorkflowInstance"/> entities.
/// </summary>
public class MemoryWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly MemoryStore<WorkflowInstance> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryWorkflowInstanceStore(MemoryStore<WorkflowInstance> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        var entities = _store.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        var page = Page.Of(entities, count);
        return Task.FromResult(page);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        var entities = _store.Query(query => Filter(query, filter).OrderBy(order).Paginate(pageArgs)).ToList();
        var page = Page.Of(entities, count);
        return Task.FromResult(page);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filter).OrderBy(order)).ToList().AsEnumerable();
        return Task.FromResult(entities);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var page = await FindManyAsync(filter, pageArgs, cancellationToken);
        return new(page.Items.Select(WorkflowInstanceSummary.FromInstance).ToList(), page.TotalCount);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var page = await FindManyAsync(filter, pageArgs, order, cancellationToken);
        return new(page.Items.Select(WorkflowInstanceSummary.FromInstance).ToList(), page.TotalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = await FindManyAsync(filter, cancellationToken);
        return entities.Select(WorkflowInstanceSummary.FromInstance);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var entities = await FindManyAsync(filter, order, cancellationToken);
        return entities.Select(WorkflowInstanceSummary.FromInstance);
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, GetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var count = await DeleteManyAsync(filter, cancellationToken);
        return count > 0;
    }

    /// <inheritdoc />
    public Task<int> DeleteManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Filter(_store.List().AsQueryable(), filter);
        var count = _store.DeleteMany(query, x => x.Id);
        return Task.FromResult(count);
    }

    private string GetId(WorkflowInstance workflowInstance) => workflowInstance.Id;

    private IQueryable<WorkflowInstance> Filter(IQueryable<WorkflowInstance> query, WorkflowInstanceFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Id)) query = query.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) query = query.Where(x => filter.Ids.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(filter.DefinitionId)) query = query.Where(x => x.DefinitionId == filter.DefinitionId);
        if (filter.DefinitionIds != null) query = query.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (filter.Version != null) query = query.Where(x => x.Version == filter.Version);
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId)) query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        if (filter.CorrelationIds != null) query = query.Where(x => filter.CorrelationIds.Contains(x.CorrelationId!));
        if (filter.WorkflowStatus != null) query = query.Where(x => x.Status == filter.WorkflowStatus);
        if (filter.WorkflowSubStatus != null) query = query.Where(x => x.SubStatus == filter.WorkflowSubStatus);

        var searchTerm = filter.SearchTerm;
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

        return query;
    }
}