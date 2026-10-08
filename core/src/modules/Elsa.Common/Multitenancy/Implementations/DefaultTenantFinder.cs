using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy;

public class DefaultTenantFinder(IServiceScopeFactory scopeFactory) : ITenantFinder
{
    public async Task<Tenant?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var tenantsProvider = scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        return await tenantsProvider.FindByIdAsync(tenantId, cancellationToken);
    }
}