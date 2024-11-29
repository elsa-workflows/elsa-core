using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

public class DefaultTenantService(IServiceScopeFactory scopeFactory) : ITenantService
{
    private IDictionary<string, Tenant>? _tenantsDictionary;

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
        
        _tenantsDictionary = newTenants;
        
        foreach (var removedTenantId in removedTenantIds)
        {
            var removedTenant = currentTenants[removedTenantId];
            //await mediator.SendAsync(new TenantRemoved(removedTenant), cancellationToken);
        }
        
        foreach (var addedTenantId in addedTenantIds)
        {
            var addedTenant = newTenants[addedTenantId];
            //await mediator.SendAsync(new TenantAdded(addedTenant), cancellationToken);
        }
    }

    private async Task<IDictionary<string, Tenant>> GetTenantsDictionaryAsync(CancellationToken cancellationToken)
    {
        if (_tenantsDictionary == null)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
            var tenants = await tenantsProvider.ListAsync(cancellationToken);
            _tenantsDictionary = tenants.ToDictionary(x => x.Id);
        }

        return _tenantsDictionary;
    }
}