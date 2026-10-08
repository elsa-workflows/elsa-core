using JetBrains.Annotations;

namespace Elsa.Common.Multitenancy;

[UsedImplicitly]
public static class TenantsProviderExtensions
{
    /// <summary>
    /// Normalizes a tenant ID by converting null to empty string, ensuring consistency with the default tenant convention.
    /// </summary>
    public static string NormalizeTenantId(this string? tenantId) => tenantId ?? Tenant.DefaultTenantId;

    public static async Task<Tenant?> FindByIdAsync(this ITenantsProvider tenantsProvider, string id, CancellationToken cancellationToken = default)
    {
        var filter = new TenantFilter
        {
            Id = id
        };
        return await tenantsProvider.FindAsync(filter, cancellationToken);
    }
}