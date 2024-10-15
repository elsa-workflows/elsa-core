using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// A hosted service that runs the start and stop methods for each tenant.
/// </summary>
[UsedImplicitly]
public abstract class MultitenantHostedService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public virtual async Task StartAsync(CancellationToken cancellationToken) => await RunThroughTenantsAsync(StartAsync, cancellationToken);
    public virtual async Task StopAsync(CancellationToken cancellationToken) => await RunThroughTenantsAsync(StopAsync, cancellationToken);

    protected virtual Task StartAsync(TenantScope tenantScope, CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task StopAsync(TenantScope tenantScope, CancellationToken cancellationToken) => Task.CompletedTask;
    
    private async Task RunThroughTenantsAsync(Func<TenantScope, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        using var serviceScope = serviceScopeFactory.CreateScope();
        var tenantScopeFactory = serviceScope.ServiceProvider.GetRequiredService<ITenantScopeFactory>();
        var tenantsProvider = serviceScope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        var tenants = (await tenantsProvider.ListAsync(cancellationToken)).ToList();
        
        foreach (var tenant in tenants)
        {
            using var tenantScope = tenantScopeFactory.CreateScope(tenant);
            await action(tenantScope, cancellationToken);
        }
    }
}