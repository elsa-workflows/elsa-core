namespace Elsa.Common.Multitenancy;

public interface ITenantAccessor
{
    /// <summary>
    /// Get the current <see cref="Tenant"/>.
    /// </summary>
    /// <returns>Current tenant or null.</returns>
    Tenant? GetTenant();
}