using Elsa.Framework.System;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Commands;
using Microsoft.Extensions.DependencyInjection;
using Timer = System.Timers.Timer;

namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// A task that is scheduled to execute at a specific instant.
/// </summary>
public class ScheduledSpecificInstantTask : IScheduledTask
{
    private readonly ITask _task;
    private readonly ISystemClock _systemClock;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DateTimeOffset _startAt;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Timer? _timer;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduledSpecificInstantTask"/>.
    /// </summary>
    public ScheduledSpecificInstantTask(ITask task, DateTimeOffset startAt, ISystemClock systemClock, IServiceScopeFactory scopeFactory)
    {
        _task = task;
        _systemClock = systemClock;
        _scopeFactory = scopeFactory;
        _startAt = startAt;
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

            using var scope = _scopeFactory.CreateScope();
            var commandSender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

            var cancellationToken = _cancellationTokenSource.Token;
            if (!cancellationToken.IsCancellationRequested)
                await commandSender.SendAsync(new RunScheduledTask(_task), cancellationToken);
        };
    }
}