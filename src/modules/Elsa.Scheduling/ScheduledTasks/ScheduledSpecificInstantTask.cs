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
    private bool _disposed;

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

        // Handle edge cases where delay is zero or negative (e.g., due to clock drift, fast execution, or time alignment)
        // Instead of silently returning, use a minimum delay to ensure the timer fires and workflow continues scheduling
        if (delay <= TimeSpan.Zero)
        {
            _logger.LogWarning("Calculated delay is {Delay} which is not positive. Using minimum delay of 1ms to ensure timer fires", delay);
            delay = TimeSpan.FromMilliseconds(1);
        }

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

            // Check disposed again before accessing CancellationTokenSource
            if (_disposed) return;

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