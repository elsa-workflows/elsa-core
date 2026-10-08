using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Workflows.ComponentTests.Scenarios.ConcurrentTriggerIndexing;

/// <summary>
/// A decorator for ITriggerStore that adds artificial delays to simulate slow database operations
/// and increase the window for race conditions to occur during testing.
/// </summary>
public class DelayedTriggerStore(ITriggerStore inner, int findDelayMs = 50, int replaceDelayMs = 50) : ITriggerStore
{
    public async ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        await Task.Delay(findDelayMs, cancellationToken);
        return await inner.FindAsync(filter, cancellationToken);
    }

    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        // Add delay BEFORE reading to maximize race condition window
        await Task.Delay(findDelayMs, cancellationToken);
        return await inner.FindManyAsync(filter, cancellationToken);
    }

    public async ValueTask<Page<StoredTrigger>> FindManyAsync(TriggerFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        await Task.Delay(findDelayMs, cancellationToken);
        return await inner.FindManyAsync(filter, pageArgs, cancellationToken);
    }

    public async ValueTask<Page<StoredTrigger>> FindManyAsync<TProp>(TriggerFilter filter, PageArgs pageArgs, StoredTriggerOrder<TProp> order, CancellationToken cancellationToken = default)
    {
        await Task.Delay(findDelayMs, cancellationToken);
        return await inner.FindManyAsync(filter, pageArgs, order, cancellationToken);
    }

    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        // Add delay BEFORE replacing to maximize race condition window
        await Task.Delay(replaceDelayMs, cancellationToken);
        await inner.ReplaceAsync(removed, added, cancellationToken);
    }

    public ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        return inner.SaveAsync(record, cancellationToken);
    }

    public ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        return inner.SaveManyAsync(records, cancellationToken);
    }

    public ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return inner.DeleteManyAsync(filter, cancellationToken);
    }
}
