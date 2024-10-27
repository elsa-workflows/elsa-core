using Elsa.Common.Multitenancy;
using Elsa.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Quartz.Listeners;

public class TenantJobListener(ITenantAccessor tenantAccessor, IServiceScopeFactory scopeFactory) : IJobListener
{
    public string Name => "TenantJobListener";
    
    public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if(!context.MergedJobDataMap.TryGetString("TenantId", out var tenantId))
            return;
        
        if (string.IsNullOrWhiteSpace(tenantId))
            return;
        
        using var scope = scopeFactory.CreateScope();
        var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        var tenant = await tenantsProvider.FindByIdAsync(tenantId, cancellationToken: cancellationToken);
        tenantAccessor.Tenant = tenant;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default) => Task.CompletedTask;
}