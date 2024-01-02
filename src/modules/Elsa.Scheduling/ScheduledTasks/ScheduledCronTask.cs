using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Commands;
using Elsa.Scheduling.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Timer = System.Timers.Timer;

namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// A task that is scheduled using a given cron expression.
/// </summary>
public class ScheduledCronTask : IScheduledTask
{
    private readonly ISystemClock _systemClock;
    private Timer? _timer;
    private readonly ITask _task;
    private readonly string _cronExpression;
    private readonly ICronParser _cronParser;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledCronTask"/>.
    /// </summary>
    public ScheduledCronTask(ITask task, string cronExpression, ICronParser cronParser, IServiceScopeFactory scopeFactory, ISystemClock systemClock)
    {
        _task = task;
        _cronExpression = cronExpression;
        _cronParser = cronParser;
        _scopeFactory = scopeFactory;
        _systemClock = systemClock;
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
        var adjusted = false;
        
        while (true)
        {
            var now = _systemClock.UtcNow;
            var nextOccurence = _cronParser.GetNextOccurrence(_cronExpression);
            var delay = nextOccurence - now;

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
        _timer = new Timer(delay.TotalMilliseconds) { Enabled = true };

        _timer.Elapsed += async (_, _) =>
        {
            _timer.Dispose();
            _timer = null;

            using var scope = _scopeFactory.CreateScope();
            var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

            var cancellationToken = _cancellationTokenSource.Token;
            if (!cancellationToken.IsCancellationRequested) await commandSender.SendAsync(new RunScheduledTask(_task), cancellationToken);
            if (!cancellationToken.IsCancellationRequested) Schedule();
        };
    } 
}