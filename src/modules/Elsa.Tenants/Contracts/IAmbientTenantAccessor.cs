namespace Elsa.Tenants;

/// <summary>
/// Provides access to the current tenant ID stored in an ambient context.
/// </summary>
public interface IAmbientTenantAccessor
{
    /// <summary>
    /// Set the current Tenant ID.
    /// </summary>
    /// <param name="tenantId">ID of the tenant</param>
    void SetCurrentTenantId(string? tenantId);

    /// <summary>
    /// Get the current Tenant ID.
    /// </summary>
    /// <returns>Current Tenant ID or null</returns>
    string? GetCurrentTenantId();
}