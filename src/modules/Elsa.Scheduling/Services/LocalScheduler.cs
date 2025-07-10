using System.Collections.Concurrent;
using System.Collections.Generic;
using Elsa.Extensions;

namespace Elsa.Scheduling.Services;

/// <summary>
/// Represents a local, in-memory scheduler that schedules tasks in-process.
/// </summary>
public class LocalScheduler : IScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, IScheduledTask> _scheduledTasks = new ConcurrentDictionary<string, IScheduledTask>();
    private readonly ConcurrentDictionary<IScheduledTask, ICollection<string>> _scheduledTaskKeys = new ConcurrentDictionary<IScheduledTask, ICollection<string>>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalScheduler"/> class.
    /// </summary>
    public LocalScheduler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public ValueTask ScheduleAsync(string name, ITask task, ISchedule schedule, CancellationToken cancellationToken = default)
    {
        return ScheduleAsync(name, task, schedule, null, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask ScheduleAsync(string name, ITask task, ISchedule schedule, IEnumerable<string>? keys = null, CancellationToken cancellationToken = default)
    {
        var scheduleContext = new ScheduleContext(_serviceProvider, task);
        var scheduledTask = schedule.Schedule(scheduleContext);

        RegisterScheduledTask(name, scheduledTask, keys);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearScheduleAsync(string name, CancellationToken cancellationToken = default)
    {
        RemoveScheduledTask(name);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearScheduleAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        RemoveScheduledTasks(keys);

        return ValueTask.CompletedTask;
    }


    private void RegisterScheduledTask(string name, IScheduledTask scheduledTask, IEnumerable<string>? keys = null)
    {
        _scheduledTasks.AddOrUpdate(
            name,
            addValueFactory: _ => scheduledTask,
            updateValueFactory: (_, existingScheduledTask) =>
            {
                existingScheduledTask.Cancel();
                var removed = _scheduledTaskKeys.TryRemove(existingScheduledTask, out ICollection<string>? _);
                if (!removed)
                    System.Diagnostics.Debug.WriteLine($"[LocalScheduler] Warning: Tried to remove scheduled task keys for an existing scheduled task, but it was not present in _scheduledTaskKeys.");
                return scheduledTask;
            });

        if (keys != null)
            _scheduledTaskKeys[scheduledTask] = keys.ToList();
    }
 

    private void RemoveScheduledTask(string name)
    {
        if (_scheduledTasks.TryGetValue(name, out var existingScheduledTask))
        {
            _scheduledTaskKeys.Remove(existingScheduledTask, out _);
            _scheduledTasks.Remove(name, out _);
            existingScheduledTask.Cancel();
        }
    }

    private void RemoveScheduledTasks(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            var scheduledTasks = _scheduledTaskKeys.Where(x => x.Value.Contains(key)).Select(x => x.Key).Distinct().ToList();

            foreach (var scheduledTask in scheduledTasks)
            {
                _scheduledTasks.RemoveWhere(x => x.Value == scheduledTask);
                _scheduledTaskKeys.Remove(scheduledTask, out _);
                scheduledTask.Cancel();
            }
        }
    }
}