using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy.EventHandlers;

public class StartRecurringTasks(RecurringTaskScheduleManager scheduleManager) : ITenantActivatedEvent, ITenantDeactivatedEvent
{
    private readonly ICollection<ScheduledTimer> _scheduledTimers = new List<ScheduledTimer>();
    private CancellationTokenSource _cancellationTokenSource = default!;

    public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        var cancellationToken = args.CancellationToken;
        _cancellationTokenSource = new CancellationTokenSource();
        var tenantScope = args.TenantScope;
        var tasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>().ToList();
        var taskExecutor = tenantScope.ServiceProvider.GetRequiredService<ITaskExecutor>();

        foreach (var task in tasks)
        {
            var schedule = scheduleManager.GetScheduleFor(task.GetType());
            var timer = schedule.CreateTimer(async () =>
            {
                await taskExecutor.ExecuteTaskAsync(task, _cancellationTokenSource.Token);
            });
            _scheduledTimers.Add(timer);
            await task.StartAsync(cancellationToken);
        }
    }

    public async Task TenantDeactivatedAsync(TenantDeactivatedEventArgs args)
    {
        var tenantScope = args.TenantScope;
        _cancellationTokenSource.Cancel();
        foreach (var timer in _scheduledTimers) await timer.DisposeAsync();
        _scheduledTimers.Clear();
        var tasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>();
        foreach (var task in tasks) await task.StopAsync(args.CancellationToken);
    }
}