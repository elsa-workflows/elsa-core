using Elsa.Framework.Tenants;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Represents a tenant provider.
/// </summary>
public interface ITenantsProvider
{
    /// <summary>
    /// Lists all the tenants.
    /// </summary>
    Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a tenant by the given filter.
    /// </summary>
    Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default);
}