using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Services;

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
    public void Clear() => _stack.Clear();
}