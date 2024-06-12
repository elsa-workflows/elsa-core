using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Commands;
using Microsoft.Extensions.DependencyInjection;
using Timer = System.Timers.Timer;

namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// A scheduled recurring task.
/// </summary>
public class ScheduledRecurringTask : IScheduledTask
{
    private readonly ITask _task;
    private readonly ISystemClock _systemClock;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private DateTimeOffset _startAt;
    private Timer? _timer;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledRecurringTask"/>.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="startAt">The instant at which to start executing the task.</param>
    /// <param name="interval">The interval at which to execute the task.</param>
    /// <param name="systemClock">The system clock.</param>
    /// <param name="scopeFactory">Scope factory to create the scope and get dependancies.</param>
    public ScheduledRecurringTask(ITask task, DateTimeOffset startAt, TimeSpan interval, ISystemClock systemClock, IServiceScopeFactory scopeFactory)
    {
        _task = task;
        _systemClock = systemClock;
        _scopeFactory = scopeFactory;
        _startAt = startAt;
        _interval = interval;
        _cancellationTokenSource = new CancellationTokenSource();

        Schedule();
    }

    /// <inheritdoc />
    public void Cancel()
    {
        _timer?.Dispose();
        _cancellationTokenSource.Cancel();
    }

    private void Schedule()
    {
        var startAt = _startAt;
        var adjusted = false;

        while (true)
        {
            var now = _systemClock.UtcNow;
            var delay = startAt - now;

            if (!adjusted && delay.Milliseconds <= 0)
            {
                adjusted = true;
                continue;
            }

            SetupTimer(delay);
            break;
        }
    }

    private void SetupTimer(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);

        _timer = new Timer(delay.TotalMilliseconds) { Enabled = true };

        _timer.Elapsed += async (_, _) =>
        {
            _timer.Dispose();
            _timer = null;
            _startAt = _systemClock.UtcNow + _interval;

            using var scope = _scopeFactory.CreateScope();
            var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

            var cancellationToken = _cancellationTokenSource.Token;
            if (!cancellationToken.IsCancellationRequested) await commandSender.SendAsync(new RunScheduledTask(_task), cancellationToken);
            if (!cancellationToken.IsCancellationRequested) Schedule();
        };
    }
}
