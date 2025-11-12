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
    private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
    private bool _executing;
    private bool _cancellationRequested;
    private bool _disposed;

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
        _cancellationTokenSource = new();

        Schedule();
    }

    /// <inheritdoc />
    public void Cancel()
    {
        _timer?.Dispose();

        if (_executing)
        {
            _cancellationRequested = true;
            return;
        }

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

            if (!adjusted && delay <= TimeSpan.Zero)
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
        // Handle edge cases where delay is zero or negative (e.g., due to clock drift, fast execution, or time alignment)
        // Instead of silently returning, use a minimum delay to ensure the timer fires and workflow continues scheduling
        if (delay <= TimeSpan.Zero)
        {
            _logger.LogWarning("Calculated delay is {Delay} which is not positive. Using minimum delay of 1ms to ensure timer fires", delay);
            delay = TimeSpan.FromMilliseconds(1);
        }

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
        _timer = new(delay.TotalMilliseconds)
        {
            Enabled = true
        };

        _timer.Elapsed += async (_, _) =>
        {
            _timer?.Dispose();
            _timer = null;

            // Check if disposed before proceeding
            if (_disposed) return;

            using var scope = _scopeFactory.CreateScope();
            var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

            var cancellationToken = _cancellationTokenSource.Token;

            if (!cancellationToken.IsCancellationRequested)
            {
                var acquired = false;
                try
                {
                    acquired = await _executionSemaphore.WaitAsync(0, cancellationToken);
                    if (!acquired) return;

                    _executing = true;
                    await commandSender.SendAsync(new RunScheduledTask(_task), cancellationToken);

                    if (_cancellationRequested)
                    {
                        _cancellationRequested = false;
                        _cancellationTokenSource.Cancel();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error executing scheduled task");
                }
                finally
                {
                    _executing = false;
                    if (acquired && !_disposed)
                        _executionSemaphore.Release();
                }
            }

            if (!cancellationToken.IsCancellationRequested && !_disposed)
                Schedule();
        };
    }

    void IDisposable.Dispose()
    {
        _disposed = true;
        _timer?.Dispose();
        _cancellationTokenSource.Dispose();
        _executionSemaphore.Dispose();
    }
}