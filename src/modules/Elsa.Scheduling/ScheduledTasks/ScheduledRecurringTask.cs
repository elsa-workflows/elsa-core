using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// A scheduled recurring task.
/// </summary>
public class ScheduledRecurringTask : IScheduledTask, IDisposable
{
    private readonly ITask _task;
    private readonly ISystemClock _systemClock;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledRecurringTask> _logger;
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
    private DateTimeOffset _startAt;
    private Timer? _timer;
    private bool _executing;
    private bool _cancellationRequested;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledRecurringTask"/>.
    /// </summary>
    public ScheduledRecurringTask(ITask task, DateTimeOffset startAt, TimeSpan interval, ISystemClock systemClock, IServiceScopeFactory scopeFactory, ILogger<ScheduledRecurringTask> logger)
    {
        _task = task;
        _systemClock = systemClock;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _startAt = startAt;
        _interval = interval;
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

        _timer = new(delay.TotalMilliseconds)
        {
            Enabled = true
        };

        _timer.Elapsed += async (_, _) =>
        {
            _timer?.Dispose();
            _timer = null;
            _startAt = _systemClock.UtcNow + _interval;

            using var scope = _scopeFactory.CreateScope();
            var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

            var cancellationToken = _cancellationTokenSource.Token;
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _executionSemaphore.WaitAsync(cancellationToken);

                    _executing = true;
                    await commandSender.SendAsync(new RunScheduledTask(_task), cancellationToken);
                    _executing = false;

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
                    _executionSemaphore.Release();
                }
            }

            if (!cancellationToken.IsCancellationRequested)
                Schedule();
        };
    }

    void IDisposable.Dispose()
    {
        _timer?.Dispose();
        _cancellationTokenSource.Dispose();
        _executionSemaphore.Dispose();
    }
}