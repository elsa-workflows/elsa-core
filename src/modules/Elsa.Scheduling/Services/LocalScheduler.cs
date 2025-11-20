using Elsa.Extensions;

namespace Elsa.Scheduling.Services;

/// <summary>
/// Represents a local, in-memory scheduler that schedules tasks in-process.
/// </summary>
public class LocalScheduler : IScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDictionary<string, IScheduledTask> _scheduledTasks = new Dictionary<string, IScheduledTask>();
    private readonly IDictionary<IScheduledTask, ICollection<string>> _scheduledTaskKeys = new Dictionary<IScheduledTask, ICollection<string>>();

    // Note: Using lock instead of SemaphoreSlim because:
    // 1. All critical sections are synchronous (dictionary operations only)
    // 2. Methods return ValueTask.CompletedTask (not truly async)
    // 3. No await inside critical sections
    // 4. lock has zero allocation overhead, perfect for fast synchronous operations
    private readonly object _lock = new();

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

        lock (_lock)
        {
            RegisterScheduledTask(name, scheduledTask, keys);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearScheduleAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            RemoveScheduledTask(name);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearScheduleAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            RemoveScheduledTasks(keys);
        }

        return ValueTask.CompletedTask;
    }

    private void RegisterScheduledTask(string name, IScheduledTask scheduledTask, IEnumerable<string>? keys = null)
    {
        if (_scheduledTasks.TryGetValue(name, out var existingScheduledTask))
        {
            existingScheduledTask.Cancel();
            _scheduledTaskKeys.Remove(existingScheduledTask);
        }

        _scheduledTasks[name] = scheduledTask;

        if (keys != null)
            _scheduledTaskKeys[scheduledTask] = keys.ToList();
    }

    private void RemoveScheduledTask(string name)
    {
        if (_scheduledTasks.TryGetValue(name, out var existingScheduledTask))
        {
            _scheduledTaskKeys.Remove(existingScheduledTask);
            _scheduledTasks.Remove(name);
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
                _scheduledTaskKeys.Remove(scheduledTask);
                scheduledTask.Cancel();
            }
        }
    }
}