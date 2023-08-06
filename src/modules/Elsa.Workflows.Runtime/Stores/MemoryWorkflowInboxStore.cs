using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// An in-memory implementation of <see cref="IWorkflowInboxStore"/>.
/// </summary>
public class MemoryWorkflowInboxStore : IWorkflowInboxStore
{
    private readonly MemoryStore<WorkflowInboxMessage> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWorkflowInboxStore"/> class.
    /// </summary>
    public MemoryWorkflowInboxStore(MemoryStore<WorkflowInboxMessage> store)
    {
        _store = store;
    }
    
    /// <inheritdoc />
    public ValueTask SaveAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        _store.Save(message, x => x.Id);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filter)).ToList();
        return new(entities);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = (await FindManyAsync(filter, cancellationToken)).Select(x => x.Id);
        return _store.DeleteMany(ids);
    }

    private static IQueryable<WorkflowInboxMessage> Filter(IQueryable<WorkflowInboxMessage> query, WorkflowInboxMessageFilter filter) => filter.Apply(query);
}