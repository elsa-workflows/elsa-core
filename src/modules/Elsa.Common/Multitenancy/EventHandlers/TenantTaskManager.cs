using System.Reflection;
using Elsa.Common.Helpers;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Common.Multitenancy.EventHandlers;

/// <summary>
/// Manages the lifecycle of startup, background, and recurring tasks for tenants.
/// Executes tasks in the proper sequence: startup tasks first, then background tasks, then recurring tasks.
/// </summary>
public class TenantTaskManager(RecurringTaskScheduleManager scheduleManager, ILogger<TenantTaskManager> logger) : ITenantActivatedEvent, ITenantDeactivatedEvent
{
    private readonly ICollection<Task> _runningBackgroundTasks = new List<Task>();
    private readonly ICollection<ScheduledTimer> _scheduledTimers = new List<ScheduledTimer>();
    private CancellationTokenSource _cancellationTokenSource = null!;

    public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        var cancellationToken = args.CancellationToken;
        var tenantScope = args.TenantScope;
        var taskExecutor = tenantScope.ServiceProvider.GetRequiredService<ITaskExecutor>();

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Step 1: Run startup tasks (with dependency ordering)
        await RunStartupTasksAsync(tenantScope, taskExecutor, cancellationToken);

        // Step 2: Run background tasks
        await RunBackgroundTasksAsync(tenantScope, taskExecutor, cancellationToken);

        // Step 3: Start recurring tasks
        await StartRecurringTasksAsync(tenantScope, taskExecutor, cancellationToken);
    }

    public async Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args)
    {
        var tenantScope = args.TenantScope;

        // Cancel all running tasks
        _cancellationTokenSource.Cancel();

        // Wait for background tasks to complete (with cancellation they should finish quickly)
        if (_runningBackgroundTasks.Any())
        {
            try
            {
                await Task.WhenAll(_runningBackgroundTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected when tasks are cancelled
            }
            _runningBackgroundTasks.Clear();
        }

        // Stop all recurring task timers
        foreach (var timer in _scheduledTimers)
            await timer.DisposeAsync();
        _scheduledTimers.Clear();

        // Stop recurring tasks
        var recurringTasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>();
        foreach (var task in recurringTasks)
            await task.StopAsync(args.CancellationToken);
    }

    private async Task RunStartupTasksAsync(ITenantScope tenantScope, ITaskExecutor taskExecutor, CancellationToken cancellationToken)
    {
        var startupTasks = tenantScope.ServiceProvider.GetServices<IStartupTask>()
            .OrderBy(x => x.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0f)
            .ToList();

        // First apply OrderAttribute to determine a base order, then perform topological sorting.
        // The topological sort is the final ordering step to ensure dependency constraints are respected.
        var sortedTasks = TopologicalTaskSorter.Sort(startupTasks).ToList();
        foreach (var task in sortedTasks)
            await taskExecutor.ExecuteTaskAsync(task, cancellationToken);
    }

    private Task RunBackgroundTasksAsync(ITenantScope tenantScope, ITaskExecutor taskExecutor, CancellationToken cancellationToken)
    {
        var backgroundTasks = tenantScope.ServiceProvider.GetServices<IBackgroundTask>();
        var backgroundTaskStarter = tenantScope.ServiceProvider.GetRequiredService<IBackgroundTaskStarter>();

        foreach (var backgroundTask in backgroundTasks)
        {
            var task = backgroundTaskStarter
                .StartAsync(backgroundTask, _cancellationTokenSource.Token)
                .ContinueWith(t => taskExecutor.ExecuteTaskAsync(backgroundTask, _cancellationTokenSource.Token),
                    cancellationToken,
                    TaskContinuationOptions.RunContinuationsAsynchronously,
                    TaskScheduler.Default)
                .Unwrap();

            if (!task.IsCompleted)
                _runningBackgroundTasks.Add(task);
        }

        return Task.CompletedTask;
    }

    private async Task StartRecurringTasksAsync(ITenantScope tenantScope, ITaskExecutor taskExecutor, CancellationToken cancellationToken)
    {
        var recurringTasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>().ToList();

        foreach (var task in recurringTasks)
        {
            var schedule = scheduleManager.GetScheduleFor(task.GetType());
            var timer = schedule.CreateTimer(async () =>
            {
                try
                {
                    await taskExecutor.ExecuteTaskAsync(task, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException e)
                {
                    logger.LogInformation(e, "Recurring task {TaskType} was cancelled", task.GetType().Name);
                }
                catch (Exception e)
                {
                    // Log but don't rethrow - recurring tasks should not crash the host
                    logger.LogError(e, "Recurring task {TaskType} failed with an error", task.GetType().Name);
                }
            }, logger);

            _scheduledTimers.Add(timer);
            await task.StartAsync(cancellationToken);
        }
    }
}
