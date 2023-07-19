using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

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
    public Task SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        _store.Save(instance, x => x.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(instances, GetId);
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Filter(_store.List().AsQueryable(), filter);
        var count = _store.DeleteMany(query, x => x.Id);
        return Task.FromResult(count);
    }

    private static string GetId(WorkflowInstance workflowInstance) => workflowInstance.Id;

    private static IQueryable<WorkflowInstance> Filter(IQueryable<WorkflowInstance> query, WorkflowInstanceFilter filter) => filter.Apply(query);
}