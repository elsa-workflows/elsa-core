using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class StackBasedActivityScheduler : IActivityScheduler
{
    private readonly Stack<ActivityWorkItem> _stack = new();

    public bool HasAny => _stack.Any();
    public void Schedule(ActivityWorkItem activity) => _stack.Push(activity);
    public ActivityWorkItem Take() => _stack.Pop();
    public IEnumerable<ActivityWorkItem> List() => _stack.ToList();
    public void Clear() => _stack.Clear();
}