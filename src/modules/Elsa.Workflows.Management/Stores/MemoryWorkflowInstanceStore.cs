using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Stores;

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
    public ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return ValueTask.FromResult(entity);
    }

    /// <inheritdoc />
    public ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        var entities = _store.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        var page = Page.Of(entities, count);
        return ValueTask.FromResult(page);
    }

    /// <inheritdoc />
    public ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        var entities = _store.Query(query => Filter(query, filter).OrderBy(order).Paginate(pageArgs)).ToList();
        var page = Page.Of(entities, count);
        return ValueTask.FromResult(page);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return ValueTask.FromResult(entities);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filter).OrderBy(order)).ToList().AsEnumerable();
        return ValueTask.FromResult(entities);
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        return new(count);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var page = await FindManyAsync(filter, pageArgs, cancellationToken);
        return new(page.Items.Select(WorkflowInstanceSummary.FromInstance).ToList(), page.TotalCount);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var page = await FindManyAsync(filter, pageArgs, order, cancellationToken);
        return new(page.Items.Select(WorkflowInstanceSummary.FromInstance).ToList(), page.TotalCount);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = await FindManyAsync(filter, cancellationToken);
        return entities.Select(WorkflowInstanceSummary.FromInstance);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var entities = await FindManyAsync(filter, order, cancellationToken);
        return entities.Select(WorkflowInstanceSummary.FromInstance);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filter)).Select(x => x.Id).ToList().AsEnumerable();
        return ValueTask.FromResult(entities);
    }

    /// <inheritdoc />
    public async ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var page = await FindManyAsync(filter, pageArgs, cancellationToken);
        var ids = page.Items.Select(x => x.Id).ToList();
        return new(ids, page.TotalCount);
    }

    /// <inheritdoc />
    public async ValueTask<Page<string>> FindManyIdsAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var page = await FindManyAsync(filter, pageArgs, order, cancellationToken);
        var ids = page.Items.Select(x => x.Id).ToList();
        return new(ids, page.TotalCount);
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        _store.Save(instance, x => x.Id);
        return ValueTask.CompletedTask;
    }

    public ValueTask AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        _store.Add(instance, GetId);
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        _store.Update(instance, GetId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(instances, GetId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Filter(_store.List().AsQueryable(), filter);
        var count = _store.DeleteMany(query, x => x.Id);
        return ValueTask.FromResult(count);
    }

    private static string GetId(WorkflowInstance workflowInstance) => workflowInstance.Id;

    [RequiresUnreferencedCode("Calls Elsa.Workflows.Management.Filters.WorkflowInstanceFilter.Apply(IQueryable<WorkflowInstance>)")]
    private static IQueryable<WorkflowInstance> Filter(IQueryable<WorkflowInstance> query, WorkflowInstanceFilter filter) => filter.Apply(query);
}