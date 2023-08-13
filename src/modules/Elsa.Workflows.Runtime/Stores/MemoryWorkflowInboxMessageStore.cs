using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime.Stores;

/// <summary>
/// An in-memory implementation of <see cref="IWorkflowInboxMessageStore"/>.
/// </summary>
public class MemoryWorkflowInboxMessageStore : IWorkflowInboxMessageStore
{
    private readonly MemoryStore<WorkflowInboxMessage> _store;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryWorkflowInboxMessageStore"/> class.
    /// </summary>
    public MemoryWorkflowInboxMessageStore(MemoryStore<WorkflowInboxMessage> store, ISystemClock systemClock)
    {
        _store = store;
        _systemClock = systemClock;
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
    public ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Filter(query, filters.ToArray())).ToList();
        return new(entities);
    }

    /// <inheritdoc />
    public ValueTask<long> DeleteManyAsync(WorkflowInboxMessageFilter filter, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var entities = _store.Query(query => Paginate(Filter(query, filter), pageArgs)).ToList();
        var ids = entities.Select(x => x.Id);
        var deleteCount = _store.DeleteMany(ids);
        return new (deleteCount);
    }

    private IQueryable<WorkflowInboxMessage> Filter(IQueryable<WorkflowInboxMessage> query, params WorkflowInboxMessageFilter[] filters)
    {
        foreach (var filter in filters) filter.Apply(query, _systemClock.UtcNow);
        return query;
    }
    
    private static IQueryable<WorkflowInboxMessage> Paginate(IQueryable<WorkflowInboxMessage> queryable, PageArgs? pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        return queryable;
    }
}