using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows;

/// <summary>
/// A FIFO queue based activity scheduler.
/// </summary>
[PublicAPI]
public class QueueBasedActivityScheduler : IActivityScheduler
{
    private readonly Queue<ActivityWorkItem> _queue = new();

    /// <inheritdoc />
    public bool HasAny => _queue.Any();

    /// <inheritdoc />
    public void Schedule(ActivityWorkItem workItem) => _queue.Enqueue(workItem);

    /// <inheritdoc />
    public ActivityWorkItem Take() => _queue.Dequeue();

    /// <inheritdoc />
    public IEnumerable<ActivityWorkItem> List() => _queue;

    /// <inheritdoc />
    public bool Any(Func<ActivityWorkItem, bool> predicate) => _queue.Any(predicate);

    /// <inheritdoc />
    public ActivityWorkItem? Find(Func<ActivityWorkItem, bool> predicate) => _queue.FirstOrDefault(predicate);

    /// <inheritdoc />
    public int RemoveWhere(Func<ActivityWorkItem, bool> predicate)
    {
        // The queue enumerates front-first, so re-enqueueing what survives restores the original order.
        var remaining = _queue.Where(x => !predicate(x)).ToList();
        var removedCount = _queue.Count - remaining.Count;

        if (removedCount == 0)
            return 0;

        _queue.Clear();

        foreach (var workItem in remaining)
            _queue.Enqueue(workItem);

        return removedCount;
    }

    /// <inheritdoc />
    public void Clear() => _queue.Clear();
}