using System.Reflection;
using Elsa.Common.DistributedHosting;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Common.Multitenancy.HostedServices;

[UsedImplicitly]
public class StartupTasksRunner(IServiceScopeFactory serviceScopeFactory, IOptions<DistributedLockingOptions> options) : MultitenantHostedService(serviceScopeFactory)
{
    protected override async Task StartAsync(TenantScope tenantScope, CancellationToken cancellationToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IStartupTask>();
        foreach (var task in tasks)
        {
            var taskType = task.GetType();
            var singleNodeTask = taskType.GetCustomAttribute<SingleNodeTaskAttribute>() != null;

            if (singleNodeTask)
            {
                var distributedLockProvider = tenantScope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
                var resourceName = taskType.AssemblyQualifiedName!;
                await using (await distributedLockProvider.AcquireLockAsync(resourceName, options.Value.LockAcquisitionTimeout, cancellationToken: cancellationToken)) 
                    await task.ExecuteAsync(cancellationToken);
            }
            else
            {
                await task.ExecuteAsync(cancellationToken);
            }
        }
    }
}