using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// A hosted service that executes a task for each tenant. 
/// </summary>
public abstract class MultitenantBackgroundService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private bool _disposed;
    protected IServiceScope ServiceScope { get; private set; } = default!;
    protected IDictionary<string, Tenant> Tenants { get; private set; } = default!;
    protected IDictionary<Tenant, TenantScope> TenantScopes { get; private set; } = default!;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        ServiceScope = serviceScopeFactory.CreateScope();
        var tenantService = ServiceScope.ServiceProvider.GetRequiredService<ITenantService>();
        var tenants = await tenantService.ListAsync(cancellationToken);
        
        await StartTenantsAsync(tenants, cancellationToken);
        await base.StartAsync(cancellationToken);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ExecuteTenantsAsync(TenantScopes.Values, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
            return;

        await StopTenantsAsync(Tenants.Values, cancellationToken);

        ServiceScope.Dispose();
        await base.StopAsync(cancellationToken);

        _disposed = true;
    }

    protected virtual Task StartAsync(TenantScope tenantScope, CancellationToken stoppingToken) => Task.CompletedTask;
    protected virtual Task ExecuteAsync(TenantScope tenantScope, CancellationToken stoppingToken) => Task.CompletedTask;
    protected virtual Task StopAsync(TenantScope tenantScope, CancellationToken stoppingToken) => Task.CompletedTask;

    private async Task StartTenantsAsync(IEnumerable<Tenant> tenants, CancellationToken cancellationToken)
    {
        var tenantScopeFactory = ServiceScope.ServiceProvider.GetRequiredService<ITenantScopeFactory>();

        foreach (var tenant in tenants)
        {
            var tenantScope = tenantScopeFactory.CreateScope(tenant);
            Tenants[tenant.Id] = tenant;
            TenantScopes[tenant] = tenantScope;
            await StartAsync(tenantScope, cancellationToken);
        }
    }
    
    private async Task ExecuteTenantsAsync(IEnumerable<TenantScope> tenantScopes, CancellationToken cancellationToken)
    {
        foreach (var tenantScope in tenantScopes)
            await ExecuteAsync(tenantScope, cancellationToken);
    }
    
    private async Task StopTenantsAsync(IEnumerable<Tenant> tenants, CancellationToken cancellationToken)
    {
        var tenantsList = tenants.ToList();
        
        foreach (var tenant in tenantsList)
        {
            var tenantScope = TenantScopes[tenant];
            TenantScopes.Remove(tenant);
            Tenants.Remove(tenant.Id);
            
            await StopAsync(tenantScope, cancellationToken);
            await tenantScope.DisposeAsync();
        }
    }
}