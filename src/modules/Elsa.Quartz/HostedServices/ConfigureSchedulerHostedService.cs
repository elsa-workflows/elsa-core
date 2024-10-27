using Elsa.Common.Multitenancy;
using Elsa.Quartz.Listeners;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Elsa.Quartz.HostedServices;

[UsedImplicitly]
public class ConfigureSchedulerHostedService(ISchedulerFactory schedulerFactory, ITenantAccessor tenantAccessor, IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        scheduler.ListenerManager.AddJobListener(new TenantJobListener(tenantAccessor, scopeFactory));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}