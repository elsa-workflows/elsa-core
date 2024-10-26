using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy.HostedServices;

[UsedImplicitly]
public class BackgroundTasksRunner(IServiceScopeFactory serviceScopeFactory) : MultitenantBackgroundService(serviceScopeFactory)
{
    protected override async Task StartAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IBackgroundTask>();
        foreach (var task in tasks) await task.StartAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IBackgroundTask>();
        var taskExecutor = tenantScope.ServiceProvider.GetRequiredService<TaskExecutor>();
        foreach (var task in tasks) await taskExecutor.ExecuteTaskAsync(task, stoppingToken);
    }
    
    protected override async Task StopAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IBackgroundTask>();
        foreach (var task in tasks) await task.StopAsync(stoppingToken);
    }
}