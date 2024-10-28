using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy.HostedServices;

[UsedImplicitly]
public class RecurringTasksRunner(IServiceScopeFactory serviceScopeFactory, RecurringTaskScheduleManager scheduleManager) : MultitenantBackgroundService(serviceScopeFactory)
{
    private readonly ICollection<ScheduledTimer> _scheduledTimers = new List<ScheduledTimer>();
    
    protected override async Task StartAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>().ToList();
        var taskExecutor = tenantScope.ServiceProvider.GetRequiredService<TaskExecutor>();
        
        foreach (var task in tasks)
        {
            var schedule = scheduleManager.GetScheduleFor(task.GetType());
            var timer = schedule.CreateTimer(async () =>
            {
                await taskExecutor.ExecuteTaskAsync(task, stoppingToken);
            });
            _scheduledTimers.Add(timer);
            await task.StartAsync(stoppingToken);
        }
    }
    
    protected override async Task StopAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        foreach (var timer in _scheduledTimers) await timer.DisposeAsync();
        _scheduledTimers.Clear();
        var tasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>();
        foreach (var task in tasks) await task.StopAsync(stoppingToken);
    }
}