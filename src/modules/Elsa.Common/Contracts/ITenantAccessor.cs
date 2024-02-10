namespace Elsa.Common.Contracts;

/// <summary>
/// Provides access to the current tenant ID.
/// </summary>
public interface ITenantAccessor
{
    /// <summary>
    /// Set the current Tenant ID.
    /// </summary>
    /// <param name="tenantId">Id of the tenant</param>
    void SetCurrentTenantId(string? tenantId);

    /// <summary>
    /// Get the current Tenant ID.
    /// </summary>
    /// <returns>Current Tenant ID or null</returns>
    string? GetCurrentTenantId();
}
