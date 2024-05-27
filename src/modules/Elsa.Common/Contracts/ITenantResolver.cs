using Elsa.Common.Entities;

namespace Elsa.Common.Contracts;

/// <summary>
/// Provides access to the current tenant ID.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Get the current <see cref="Tenant"/>.
    /// </summary>
    /// <returns>Current tenant or null.</returns>
    Task<Tenant?> GetTenantAsync(CancellationToken cancellationToken = default);
}