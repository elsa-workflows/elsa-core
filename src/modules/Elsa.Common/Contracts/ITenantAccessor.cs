namespace Elsa.Common.Contracts;
public interface ITenantAccessor
{
    /// <summary>
    /// Set the current Tenant Id. Used mostly by WorkflowRunner for background execution
    /// </summary>
    /// <param name="tenantId">Id of the tenant</param>
    void SetCurrentTenantId(string? tenantId);

    /// <summary>
    /// Get the current TenantId
    /// </summary>
    /// <returns>Current TenantId or null</returns>
    string? GetCurrentTenantId();
}
