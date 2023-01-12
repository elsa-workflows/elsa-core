using Elsa.Common.Implementations;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

/// <inheritdoc />
public class MemoryTriggerStore : ITriggerStore
{
    private readonly MemoryStore<StoredTrigger> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryTriggerStore(MemoryStore<StoredTrigger> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return new();
    }

    /// <inheritdoc />
    public ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, x => x.Id);
        return new();
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredTrigger>> FindAsync(string hash, CancellationToken cancellationToken = default)
    {
        var triggers = _store.Query(query => query.Where(x => x.Hash == hash));
        return new(triggers.ToList());
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredTrigger>> FindByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var triggers = _store.Query(query => query.Where(x => x.WorkflowDefinitionId == workflowDefinitionId));
        return new(triggers.ToList());
    }

    /// <inheritdoc />
    public ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        _store.DeleteMany(removed, x => x.Id);
        _store.SaveMany(added, x => x.Id);
        return new();
    }

    /// <inheritdoc />
    public ValueTask DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        _store.DeleteMany(ids);
        return new();
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<StoredTrigger>> FindByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default)
    {
        var triggers = _store.Query(query => query.Where(x => x.Name == activityType)).ToList();
        return new(triggers);
    }
}