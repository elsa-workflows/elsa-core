using Elsa.Common.Multitenancy;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Maps the tenant id of a stored record to the id of the tenant that processes it.
/// </summary>
internal static class TenantKey
{
    /// <summary>
    /// Maps null, whitespace and the tenant-agnostic id to the default tenant id; any other id is returned as is.
    /// </summary>
    public static string From(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) || tenantId == Tenant.AgnosticTenantId ? Tenant.DefaultTenantId : tenantId;
}
