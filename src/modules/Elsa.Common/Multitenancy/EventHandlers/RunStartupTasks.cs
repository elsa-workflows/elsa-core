using Elsa.Common.Multitenancy.HostedServices;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy.EventHandlers;

public class RunStartupTasks : ITenantActivatedEvent
{
    public async Task TenantActivatedAsync(TenantActivatedEventArgs args)
    {
        var cancellationToken = args.CancellationToken;       
        var tenantScope = args.TenantScope;
        var tasks = tenantScope.ServiceProvider.GetServices<IStartupTask>();
        var taskExecutor = tenantScope.ServiceProvider.GetRequiredService<ITaskExecutor>();
        
        foreach (var task in tasks) 
            await taskExecutor.ExecuteTaskAsync(task, cancellationToken);
    }
}