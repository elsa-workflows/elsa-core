using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// A task that is scheduled to execute at a specific instant.
/// </summary>
public class ScheduledSpecificInstantTask : IScheduledTask, IDisposable
{
    private readonly ITask _task;
    private readonly ISystemClock _systemClock;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledSpecificInstantTask> _logger;
    private readonly DateTimeOffset _startAt;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
    private Timer? _timer;
    private bool _executing;
    private bool _cancellationRequested;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledSpecificInstantTask"/>.
    /// </summary>
    public ScheduledSpecificInstantTask(ITask task, DateTimeOffset startAt, ISystemClock systemClock, IServiceScopeFactory scopeFactory, ILogger<ScheduledSpecificInstantTask> logger)
    {
        _task = task;
        _systemClock = systemClock;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _startAt = startAt;
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
        var now = _systemClock.UtcNow;
        var delay = _startAt - now;

        if (delay.Milliseconds <= 0)
            delay = TimeSpan.FromMilliseconds(1);

        _timer = new(delay.TotalMilliseconds)
        {
            Enabled = true
        };

        _timer.Elapsed += async (_, _) =>
        {
            _timer?.Dispose();
            _timer = null;

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
                    _logger.LogError(e, "Error scheduled task");
                }
                finally
                {
                    _executionSemaphore.Release();
                }
            }
        };
    }

    void IDisposable.Dispose()
    {
        _cancellationTokenSource.Dispose();
        _timer?.Dispose();
    }
}