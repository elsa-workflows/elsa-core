using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// A task that is scheduled using a given cron expression.
/// </summary>
public class ScheduledCronTask : IScheduledTask, IDisposable
{
    private readonly ISystemClock _systemClock;
    private readonly ILogger _logger;
    private Timer? _timer;
    private readonly ITask _task;
    private readonly string _cronExpression;
    private readonly ICronParser _cronParser;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledCronTask"/>.
    /// </summary>
    public ScheduledCronTask(ITask task, string cronExpression, ICronParser cronParser, IServiceScopeFactory scopeFactory, ISystemClock systemClock, ILogger<ScheduledCronTask> logger)
    {
        _task = task;
        _cronExpression = cronExpression;
        _cronParser = cronParser;
        _scopeFactory = scopeFactory;
        _systemClock = systemClock;
        _logger = logger;
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

            TrySetupTimer(delay);
            break;
        }
    }

    private void TrySetupTimer(TimeSpan delay)
    {
        if (delay.Milliseconds <= 0) 
            return;

        try
        {
            SetupTimer(delay);
        }
        catch (ArgumentException e)
        {
            _logger.LogError(e, "Failed to setup timer for scheduled task");
        }
    }

    private void SetupTimer(TimeSpan delay)
    {
        _timer = new Timer(delay.TotalMilliseconds) { Enabled = true };

        _timer.Elapsed += async (_, _) =>
        {
            _timer?.Dispose();
            _timer = null;

            using var scope = _scopeFactory.CreateScope();
            var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

            var cancellationToken = _cancellationTokenSource.Token;
            if (!cancellationToken.IsCancellationRequested) await commandSender.SendAsync(new RunScheduledTask(_task), cancellationToken);
            if (!cancellationToken.IsCancellationRequested) Schedule();
        };
    }

    void IDisposable.Dispose()
    {
        _timer?.Dispose();
        _cancellationTokenSource.Dispose();
    }
}