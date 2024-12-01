using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

public class DefaultTenantService(IServiceScopeFactory scopeFactory, ITenantScopeFactory tenantScopeFactory, TenantEventsManager tenantEvents) : ITenantService, IAsyncDisposable
{
    private readonly AsyncServiceScope _serviceScope = scopeFactory.CreateAsyncScope();
    private IDictionary<string, Tenant>? _tenantsDictionary;
    private IDictionary<Tenant, TenantScope>? _tenantScopesDictionary;

    public async ValueTask DisposeAsync()
    {
        await _serviceScope.DisposeAsync();
    }

    public async Task<Tenant?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return dictionary.TryGetValue(id, out var tenant) ? tenant : null;
    }

    public async Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return filter.Apply(dictionary.Values.AsQueryable()).FirstOrDefault();
    }

    public async Task<Tenant> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return dictionary[id];
    }

    public async Task<Tenant> GetAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return filter.Apply(dictionary.Values.AsQueryable()).First();
    }

    public async Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return dictionary.Values;
    }

    public async Task<IEnumerable<Tenant>> ListAsync(TenantFilter filter, CancellationToken cancellationToken = default)
    {
        var dictionary = await GetTenantsDictionaryAsync(cancellationToken);
        return filter.Apply(dictionary.Values.AsQueryable());
    }

    public async Task ActivateTenantsAsync(CancellationToken cancellationToken = default)
    {
        await RefreshAsync(cancellationToken);
    }

    public async Task DeactivateTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = _tenantsDictionary!.Values.ToArray();
        
        foreach (var tenant in tenants)
            await UnregisterTenantAsync(tenant, cancellationToken);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        var currentTenants = await GetTenantsDictionaryAsync(cancellationToken);
        var currentTenantIds = currentTenants.Keys;
        var newTenants = (await tenantsProvider.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var newTenantIds = newTenants.Keys;
        var removedTenantIds = currentTenantIds.Except(newTenantIds).ToArray();
        var addedTenantIds = newTenantIds.Except(currentTenantIds).ToArray();

        foreach (var removedTenantId in removedTenantIds)
        {
            var removedTenant = currentTenants[removedTenantId];
            await UnregisterTenantAsync(removedTenant, cancellationToken);
        }

        foreach (var addedTenantId in addedTenantIds)
        {
            var addedTenant = newTenants[addedTenantId];
            await RegisterTenantAsync(addedTenant, cancellationToken);
        }
    }

    private async Task<IDictionary<string, Tenant>> GetTenantsDictionaryAsync(CancellationToken cancellationToken)
    {
        if (_tenantsDictionary == null)
        {
            _tenantsDictionary = new Dictionary<string, Tenant>();
            _tenantScopesDictionary = new Dictionary<Tenant, TenantScope>();
            var tenantsProvider = _serviceScope.ServiceProvider.GetRequiredService<ITenantsProvider>();
            var tenants = await tenantsProvider.ListAsync(cancellationToken);

            foreach (var tenant in tenants)
                await RegisterTenantAsync(tenant, cancellationToken);
        }

        return _tenantsDictionary;
    }

    private async Task RegisterTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var scope = tenantScopeFactory.CreateScope(tenant);
        _tenantsDictionary![tenant.Id] = tenant;
        _tenantScopesDictionary![tenant] = scope;

        await tenantEvents.TenantActivatedAsync(new TenantActivatedEventArgs(tenant, scope, cancellationToken));
    }

    private async Task UnregisterTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        var scope = _tenantScopesDictionary![tenant];
        _tenantsDictionary!.Remove(tenant.Id);
        _tenantScopesDictionary!.Remove(tenant);

        await tenantEvents.TenantDeactivatedAsync(new TenantDeactivatedEventArgs(tenant, scope, cancellationToken));
    }
}