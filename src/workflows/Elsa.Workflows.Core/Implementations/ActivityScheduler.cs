using Elsa.Models;
using Elsa.Services;

namespace Elsa.Implementations;

public class ActivityScheduler : IActivityScheduler
{
    private readonly Stack<ActivityWorkItem> _stack = new();

    public bool HasAny => _stack.Any();
    public void Push(ActivityWorkItem activity) => _stack.Push(activity);
    public ActivityWorkItem Pop() => _stack.Pop();
    public IEnumerable<ActivityWorkItem> List() => _stack.ToList();
    public void Clear() => _stack.Clear();
}