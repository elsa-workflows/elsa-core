using Elsa.Common.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class MemoryWorkflowStateStore : IWorkflowStateStore
{
    private readonly MemoryStore<WorkflowState> _store;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWorkflowStateStore"/> class.
    /// </summary>
    public MemoryWorkflowStateStore(MemoryStore<WorkflowState> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default)
    {
        _store.Save(state, x => x.Id);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<WorkflowState?> FindAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        var entity = _store.Query(query => Filter(query, filter)).FirstOrDefault();
        return new(entity);
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        var count = _store.Query(query => Filter(query, filter)).LongCount();
        return new(count);
    }

    /// <inheritdoc />
    public Task<long> DeleteManyAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filter)).ToList();
        var count = _store.DeleteMany(entities, x => x.Id);
        return Task.FromResult(count);
    }

    private static IQueryable<WorkflowState> Filter(IQueryable<WorkflowState> query, WorkflowStateFilter filter) => filter.Apply(query);
}