using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// A hosted service that executes a task for each tenant. 
/// </summary>
/// <param name="serviceScopeFactory"></param>
public abstract class MultitenantBackgroundService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var serviceScope = serviceScopeFactory.CreateScope();
        var tenantScopeFactory = serviceScope.ServiceProvider.GetRequiredService<ITenantScopeFactory>();
        var tenantsProvider = serviceScope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        var tenants = (await tenantsProvider.ListAsync(stoppingToken)).ToList();

        foreach (var tenant in tenants)
        {
            using var tenantScope = tenantScopeFactory.CreateScope(tenant);
            await ExecuteAsync(tenantScope, stoppingToken);
        }
    }

    protected virtual Task ExecuteAsync(TenantScope tenantScope, CancellationToken stoppingToken) => Task.CompletedTask;
}