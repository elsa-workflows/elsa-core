using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Commands;
using Elsa.Scheduling.Contracts;
using Timer = System.Timers.Timer;

namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// A task that is scheduled to execute at a specific instant.
/// </summary>
public class ScheduledSpecificInstantTask : IScheduledTask
{
    private readonly ITask _task;
    private readonly ISystemClock _systemClock;
    private readonly DateTimeOffset _startAt;
    private readonly IBackgroundCommandSender _commandSender;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Timer? _timer;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledSpecificInstantTask"/>.
    /// </summary>
    public ScheduledSpecificInstantTask(ITask task, DateTimeOffset startAt, IBackgroundCommandSender commandSender, ISystemClock systemClock)
    {
        _task = task;
        _systemClock = systemClock;
        _startAt = startAt;
        _commandSender = commandSender;
        _cancellationTokenSource = new CancellationTokenSource();

        Schedule();
    }

    /// <inheritdoc />
    public void Cancel() => _timer?.Dispose();

    private void Schedule()
    {
        var now = _systemClock.UtcNow;
        var delay = _startAt - now;

        if (delay.Milliseconds <= 0)
            delay = TimeSpan.FromMilliseconds(1);

        _timer = new Timer(delay.TotalMilliseconds) { Enabled = true };

        _timer.Elapsed += async (_, _) =>
        {
            _timer?.Dispose();
            _timer = null;

            var cancellationToken = _cancellationTokenSource.Token;
            if (!cancellationToken.IsCancellationRequested)
                await _commandSender.SendAsync(new RunScheduledTask(_task), cancellationToken);
        };
    }
}