using System.Collections.Concurrent;
using System.Collections.Generic;
using Elsa.Extensions;
using Microsoft.Extensions.Logging;

namespace Elsa.Scheduling.Services;

/// <summary>
/// Represents a local, in-memory scheduler that schedules tasks in-process.
/// </summary>
public class LocalScheduler : IScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LocalScheduler> _logger;
    private readonly ConcurrentDictionary<string, IScheduledTask> _scheduledTasks = new();
    private readonly ConcurrentDictionary<IScheduledTask, ICollection<string>> _scheduledTaskKeys = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalScheduler"/> class.
    /// </summary>
    public LocalScheduler(IServiceProvider serviceProvider, ILogger<LocalScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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
                var removed = _scheduledTaskKeys.TryRemove(existingScheduledTask, out var _);
                if (!removed)
                    _logger.LogWarning("Tried to remove scheduled task keys for an existing scheduled task, but it was not present in _scheduledTaskKeys");
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
                // Collect all keys in _scheduledTasks that map to this scheduledTask
                var matchingTaskKeys = _scheduledTasks.Where(x => x.Value == scheduledTask).Select(x => x.Key).ToList();
                foreach (var taskKey in matchingTaskKeys)
                {
                    var removed = _scheduledTasks.TryRemove(taskKey, out _);
                    if (!removed)
                        _logger.LogWarning("Failed to remove scheduled task with key '{TaskKey}' for '{Key}' from _scheduledTasks", taskKey, key);
                }

                _scheduledTaskKeys.Remove(scheduledTask, out _);
                scheduledTask.Cancel();
            }
        }
    }
}