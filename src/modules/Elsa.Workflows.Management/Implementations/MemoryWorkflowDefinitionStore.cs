using Elsa.Common.Implementations;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

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
    public Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
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
    public Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
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
    public Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken)
    {
        var result = _store.Query(query => Filter(query, filter)).MaxBy(x => x.Version);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, GetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, GetId);
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
    
    private IQueryable<WorkflowDefinition> Filter(IQueryable<WorkflowDefinition> queryable, WorkflowDefinitionFilter filter)
    {
        if (filter.DefinitionId != null) queryable = queryable.Where(x => x.DefinitionId == filter.DefinitionId);
        if (filter.DefinitionIds != null) queryable = queryable.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id));
        if (filter.VersionOptions != null) queryable = queryable.WithVersion(filter.VersionOptions.Value);
        if (filter.MaterializerName != null) queryable = queryable.Where(x => x.MaterializerName == filter.MaterializerName);
        if (filter.Name != null) queryable = queryable.Where(x => x.Name == filter.Name);
        if (filter.Names != null) queryable = queryable.Where(x => filter.Names.Contains(x.Name));
        if (filter.UsableAsActivity != null) queryable = queryable.Where(x => x.UsableAsActivity == filter.UsableAsActivity);
        
        return queryable;
    }

    private string GetId(WorkflowDefinition workflowDefinition) => workflowDefinition.Id;
}