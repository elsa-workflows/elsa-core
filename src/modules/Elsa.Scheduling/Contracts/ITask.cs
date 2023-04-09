using Elsa.Scheduling.Models;

namespace Elsa.Scheduling.Contracts;

/// <summary>
/// Represents a task.
/// </summary>
public interface ITask
{
    /// <summary>
    /// Executes the task.
    /// </summary>
    ValueTask ExecuteAsync(TaskExecutionContext context);
}