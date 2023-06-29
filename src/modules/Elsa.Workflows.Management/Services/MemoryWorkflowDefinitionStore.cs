using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// A memory implementation of <see cref="IWorkflowDefinitionStore"/>.
/// </summary>
public class MemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly MemoryStore<WorkflowDefinition> _store;
    private readonly MemoryStore<WorkflowInstance> _instanceStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryWorkflowDefinitionStore(
        MemoryStore<WorkflowDefinition> store,
        MemoryStore<WorkflowInstance> instanceStore)
    {
        _store = store;
        _instanceStore = instanceStore;
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter).OrderBy(order)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        var result = _store.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = _store.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter).OrderBy(order)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        var result = _store.Query(query => Filter(query, filter).Paginate(pageArgs)).Select(WorkflowDefinitionSummary.FromDefinition).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter).OrderBy(order)).LongCount();
        var result = _store.Query(query => Filter(query, filter).Paginate(pageArgs)).Select(WorkflowDefinitionSummary.FromDefinition).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter)).Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync<TOrderBy>(WorkflowDefinitionFilter filter, WorkflowDefinitionOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = _store.Query(query => Filter(query, filter).OrderBy(order)).Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var result = _store.Query(query => Filter(query, filter)).MaxBy(x => x.Version);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        _store.Save(definition, GetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> definitions, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(definitions, GetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var workflowDefinitionIds = _store.Query(query => Filter(query, filter)).Select(x => x.DefinitionId).Distinct().ToList();
        _instanceStore.DeleteWhere(x => workflowDefinitionIds.Contains(x.DefinitionId));
        _store.DeleteWhere(x => workflowDefinitionIds.Contains(x.DefinitionId));
        return Task.FromResult(workflowDefinitionIds.Count);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var exists = _store.Query(query => Filter(query, filter)).Any();
        return Task.FromResult(exists);
    }

    public Task<long> CountDistinctAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private IQueryable<WorkflowDefinition> Filter(IQueryable<WorkflowDefinition> queryable, WorkflowDefinitionFilter filter) => filter.Apply(queryable);

    private string GetId(WorkflowDefinition workflowDefinition) => workflowDefinition.Id;
}