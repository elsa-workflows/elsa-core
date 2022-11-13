using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class QueueBasedActivityScheduler : IActivityScheduler
{
    private readonly Queue<ActivityWorkItem> _queue = new();

    public bool HasAny => _queue.Any();
    public void Schedule(ActivityWorkItem activity) => _queue.Enqueue(activity);
    public ActivityWorkItem Take() => _queue.Dequeue();
    public IEnumerable<ActivityWorkItem> List() => _queue.ToList();
    public void Clear() => _queue.Clear();
}