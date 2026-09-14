using Elsa.Common.Multitenancy;

namespace Elsa.Secrets.Repositories;

/// <summary>
/// Applies the same ambient-tenant visibility and write rules used by EF Core persistence to the
/// default secret repositories.
/// </summary>
internal static class SecretRepositoryTenant
{
    public static string CurrentTenantId(ITenantAccessor? tenantAccessor) =>
        tenantAccessor?.TenantId ?? Tenant.DefaultTenantId;

    public static void Stamp(Secret secret, ITenantAccessor? tenantAccessor)
    {
        if (tenantAccessor is null || secret.TenantId == Tenant.AgnosticTenantId)
            return;

        secret.TenantId ??= tenantAccessor.TenantId;
    }

    public static bool IsVisible(Secret secret, ITenantAccessor? tenantAccessor) =>
        TenantVisibility.IsVisible(secret.TenantId, CurrentTenantId(tenantAccessor));

    public static bool CanReplace(Secret existing, Secret incoming, ITenantAccessor? tenantAccessor) =>
        TenantVisibility.CanReplaceOwnedRow(existing.TenantId, incoming.TenantId, CurrentTenantId(tenantAccessor));

    public static bool HasName(Secret secret, string name) =>
        string.Equals(secret.Name, name, StringComparison.OrdinalIgnoreCase);

    public static bool HasSameTenantName(Secret existing, Secret incoming) =>
        existing.TenantId == incoming.TenantId && HasName(existing, incoming.Name);
}
