namespace Elsa.Common.Multitenancy;

public interface ITenantService
{
    /// <summary>
    /// Finds a tenant by the given id.
    /// </summary>
    /// <param name="id">The tenant id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant or null if not found.</returns>
    Task<Tenant?> FindAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a tenant using the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<Tenant?> FindAsync(TenantFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a tenant by the given id.
    /// </summary>
    /// <param name="id">The tenant id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the tenant is not found.</exception>
    Task<Tenant> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a tenant by the given id.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the tenant is not found.</exception>
    Task<Tenant> GetAsync(TenantFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists all the tenants.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenants.</returns>
    Task<IEnumerable<Tenant>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all the tenants using the given filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenants.</returns>
    Task<IEnumerable<Tenant>> ListAsync(TenantFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activates all tenants.
    /// </summary>
    Task ActivateTenantsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deactivates all tenants.
    /// </summary>
    Task DeactivateTenantsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invokes the <see cref="ITenantsProvider"/> and caches the result.
    /// When new tenants are added, lifecycle events are triggered to ensure background tasks are updated.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}