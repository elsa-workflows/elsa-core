using Elsa.Framework.Tenants;
using JetBrains.Annotations;

namespace Elsa.Common.Multitenancy;

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