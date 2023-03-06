using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public class QueueBasedActivityScheduler : IActivityScheduler
{
    private readonly Queue<ActivityWorkItem> _queue = new();

    public bool HasAny => _queue.Any();
    public void Schedule(ActivityWorkItem activity) => _queue.Enqueue(activity);
    public ActivityWorkItem Take() => _queue.Dequeue();
    public IEnumerable<ActivityWorkItem> List() => _queue.ToList();
    public void Clear() => _queue.Clear();
}