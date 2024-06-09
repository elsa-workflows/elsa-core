namespace Elsa.Framework.Tenants;

/// <summary>
/// Represents a tenant provider.
/// </summary>
public interface ITenantsProvider
{
    /// <summary>
    /// Lists all the tenants.
    /// </summary>
    ValueTask<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a tenant by the given filter.
    /// </summary>
    ValueTask<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default);
}
