using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy.HostedServices;

public class RecurringTasksRunner(IServiceScopeFactory serviceScopeFactory) : MultitenantRecurringTaskService(serviceScopeFactory)
{
    protected override async Task StartAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>();
        foreach (var task in tasks) await task.StartAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>();
        foreach (var task in tasks) await task.ExecuteAsync(stoppingToken);
    }
    
    protected override async Task StopAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>();
        foreach (var task in tasks) await task.StopAsync(stoppingToken);
    }
}