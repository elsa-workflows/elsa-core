using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows;

/// <summary>
/// A LIFO stack based activity scheduler.
/// </summary>
[PublicAPI]
public class StackBasedActivityScheduler : IActivityScheduler
{
    private readonly Stack<ActivityWorkItem> _stack = new();

    /// <inheritdoc />
    public bool HasAny => _stack.Any();

    /// <inheritdoc />
    public void Schedule(ActivityWorkItem activity) => _stack.Push(activity);

    /// <inheritdoc />
    public ActivityWorkItem Take() => _stack.Pop();

    /// <inheritdoc />
    public IEnumerable<ActivityWorkItem> List() => _stack.ToList();

    /// <inheritdoc />
    public bool Any(Func<ActivityWorkItem, bool> predicate) => _stack.Any(predicate);

    /// <inheritdoc />
    public ActivityWorkItem? Find(Func<ActivityWorkItem, bool> predicate) => _stack.FirstOrDefault(predicate);

    /// <inheritdoc />
    public int RemoveWhere(Func<ActivityWorkItem, bool> predicate)
    {
        // The stack enumerates top-first, so what survives has to be pushed back in reverse to keep the same top.
        var remaining = _stack.Where(x => !predicate(x)).ToList();
        var removedCount = _stack.Count - remaining.Count;

        if (removedCount == 0)
            return 0;

        _stack.Clear();

        for (var i = remaining.Count - 1; i >= 0; i--)
            _stack.Push(remaining[i]);

        return removedCount;
    }

    /// <inheritdoc />
    public void Clear() => _stack.Clear();
}