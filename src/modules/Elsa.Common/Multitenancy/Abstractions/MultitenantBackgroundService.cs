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
    protected ICollection<Tenant> Tenants { get; private set; } = default!;
    protected IDictionary<Tenant, TenantScope> TenantScopes { get; private set; } =  default!;
    
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        ServiceScope = serviceScopeFactory.CreateScope();
        var tenantScopeFactory = ServiceScope.ServiceProvider.GetRequiredService<ITenantScopeFactory>();
        var tenantsProvider = ServiceScope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        Tenants = (await tenantsProvider.ListAsync(cancellationToken)).ToList();
        TenantScopes = Tenants.ToDictionary(x => x, x => tenantScopeFactory.CreateScope(x));

        foreach (var entry in TenantScopes) 
            await StartAsync(entry.Value, cancellationToken);
        
        await base.StartAsync(cancellationToken);
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if(_disposed)
            return;
        
        foreach (var entry in TenantScopes) 
            await StopAsync(entry.Value, cancellationToken);
        
        foreach (var entry in TenantScopes) 
            await entry.Value.DisposeAsync();
        
        ServiceScope.Dispose();
        await base.StopAsync(cancellationToken);
        
        _disposed = true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var entry in TenantScopes) 
            await ExecuteAsync(entry.Value, stoppingToken);
    }

    protected virtual Task StartAsync(TenantScope tenantScope, CancellationToken stoppingToken) => Task.CompletedTask;
    protected virtual Task ExecuteAsync(TenantScope tenantScope, CancellationToken stoppingToken) => Task.CompletedTask;
    protected virtual Task StopAsync(TenantScope tenantScope, CancellationToken stoppingToken) => Task.CompletedTask;
}