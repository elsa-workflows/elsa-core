using Elsa.Common.Multitenancy;
using JetBrains.Annotations;

namespace Elsa.Tenants;

[UsedImplicitly]
public static class TenantsProviderExtensions
{
    public static async Task<Tenant?> FindByIdAsync(this ITenantsProvider tenantsProvider, string id, CancellationToken cancellationToken = default)
    {
        var filter = new TenantFilter
        {
            Id = id
        };
        return await tenantsProvider.FindAsync(filter, cancellationToken);
    }
}