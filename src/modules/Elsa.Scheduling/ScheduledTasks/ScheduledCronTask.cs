using Cronos;
using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Handlers;
using Timer = System.Timers.Timer;

namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// A task that is scheduled using a given cron expression.
/// </summary>
public class ScheduledCronTask : IScheduledTask
{
    private readonly ISystemClock _systemClock;
 
    private readonly CronExpression _parsedCronExpression;
    private Timer? _timer;
    private readonly ITask _task;
    private readonly IBackgroundCommandSender _commandSender;
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledCronTask"/>.
    /// </summary>
    public ScheduledCronTask(ITask task, string cronExpression, IBackgroundCommandSender commandSender, ISystemClock systemClock)
    {
        _task = task;
        _commandSender = commandSender;
        _systemClock = systemClock;
        _parsedCronExpression = CronExpression.Parse(cronExpression);
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
        while (true)
        {
            var now = _systemClock.UtcNow;
            var nextOccurence = _parsedCronExpression.GetNextOccurrence(now.UtcDateTime)!;
            var delay = nextOccurence.Value - now;

            if (delay.Milliseconds <= 0)
                continue;

            SetupTimer(delay);
            break;
        }
    }

    private void SetupTimer(TimeSpan delay)
    {
        _timer = new Timer(delay.TotalMilliseconds) { Enabled = true };

        _timer.Elapsed += async (_, _) =>
        {
            _timer.Dispose();
            _timer = null;

            var cancellationToken = _cancellationTokenSource.Token;
            if (!cancellationToken.IsCancellationRequested) await _commandSender.SendAsync(new RunScheduledTask(_task), cancellationToken);
            if (!cancellationToken.IsCancellationRequested) Schedule();
        };
    } 
}