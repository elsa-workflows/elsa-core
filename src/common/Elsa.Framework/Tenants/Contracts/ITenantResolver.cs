namespace Elsa.Framework.Tenants.Contracts;

/// <summary>
/// Provides access to the current tenant ID.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Get the current <see cref="Tenant"/>.
    /// </summary>
    /// <returns>Current tenant.</returns>
    Task<Tenant> GetTenantAsync(CancellationToken cancellationToken = default);
}