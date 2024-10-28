using Elsa.Common.DistributedHosting;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Common.Multitenancy.HostedServices;

[UsedImplicitly]
public class StartupTasksRunner(IServiceScopeFactory serviceScopeFactory, IOptions<DistributedLockingOptions> options) : MultitenantHostedService(serviceScopeFactory)
{
    protected override async Task StartAsync(TenantScope tenantScope, CancellationToken cancellationToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IStartupTask>();
        var taskExecutor = tenantScope.ServiceProvider.GetRequiredService<TaskExecutor>();
        foreach (var task in tasks) 
            await taskExecutor.ExecuteTaskAsync(task, cancellationToken);
    }
}