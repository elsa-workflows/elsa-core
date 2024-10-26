using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Common.Multitenancy;

public abstract class MultitenantRecurringTaskService(IServiceScopeFactory serviceScopeFactory) : MultitenantBackgroundService(serviceScopeFactory)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            var distributedLockProvider = ServiceScope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
            var lockAcquisitionTimeout = TimeSpan.FromSeconds(60);
            await using (await distributedLockProvider.AcquireLockAsync(GetType().AssemblyQualifiedName!, lockAcquisitionTimeout, stoppingToken))
            {
                await base.ExecuteAsync(stoppingToken);
            }
        }
    }
}