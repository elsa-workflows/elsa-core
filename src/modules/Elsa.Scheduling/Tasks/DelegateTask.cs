using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using JetBrains.Annotations;

namespace Elsa.Scheduling.Tasks;

/// <summary>
/// A generic task that executes the provided delegate.
/// </summary>
[PublicAPI]
public class DelegateTask : ITask
{
    private readonly Func<TaskExecutionContext, ValueTask> _task;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateTask"/> class.
    /// </summary>
    /// <param name="task">The delegate to execute.</param>
    public DelegateTask(Func<TaskExecutionContext, ValueTask> task) => _task = task;

    /// <inheritdoc />
    public ValueTask ExecuteAsync(TaskExecutionContext context) => _task(context);
}