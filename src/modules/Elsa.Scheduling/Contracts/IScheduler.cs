namespace Elsa.Scheduling.Contracts;

/// <summary>
/// Schedules tasks.
/// </summary>
public interface IScheduler
{
    /// <summary>
    /// Schedules a task using the specified schedule.
    /// </summary>
    /// <param name="name">The name of the task to schedule.</param>
    /// <param name="task">The task to schedule.</param>
    /// <param name="schedule">The schedule to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ScheduleAsync(string name, ITask task, ISchedule schedule, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedules a task using the specified schedule.
    /// </summary>
    /// <param name="name">The name of the task to schedule.</param>
    /// <param name="task">The task to schedule.</param>
    /// <param name="schedule">The schedule to use.</param>
    /// <param name="keys">A list of keys to associate with the task.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ScheduleAsync(string name, ITask task, ISchedule schedule, IEnumerable<string>? keys = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unschedules the task with the specified name.
    /// </summary>
    /// <param name="name">The name of the task to unschedule.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ClearScheduleAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unschedules tasks with the specified keys.
    /// </summary>
    /// <param name="keys">The keys of the tasks to unschedule.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ClearScheduleAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}