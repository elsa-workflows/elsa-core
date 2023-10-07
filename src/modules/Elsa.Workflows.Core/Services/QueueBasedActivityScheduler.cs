using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Services;

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
    public IEnumerable<ActivityWorkItem> List() => _queue.ToList();

    /// <inheritdoc />
    public bool Any(Func<ActivityWorkItem, bool> predicate) => _queue.Any(predicate);

    /// <inheritdoc />
    public ActivityWorkItem? Find(Func<ActivityWorkItem, bool> predicate) => _queue.FirstOrDefault(predicate);

    /// <inheritdoc />
    public void Clear() => _queue.Clear();
}