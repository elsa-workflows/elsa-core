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

    public static void Stamp(Secret secret, ITenantAccessor? tenantAccessor, bool tenancyEnabled)
    {
        if (!tenancyEnabled || tenantAccessor is null || secret.TenantId == Tenant.AgnosticTenantId)
            return;

        secret.TenantId ??= tenantAccessor.TenantId;
    }

    public static bool IsVisible(Secret secret, ITenantAccessor? tenantAccessor, bool tenancyEnabled) =>
        !tenancyEnabled || TenantVisibility.IsVisible(secret.TenantId, CurrentTenantId(tenantAccessor));

    public static bool CanReplace(Secret existing, Secret incoming, ITenantAccessor? tenantAccessor, bool tenancyEnabled) =>
        !tenancyEnabled || TenantVisibility.CanReplaceOwnedRow(existing.TenantId, incoming.TenantId, CurrentTenantId(tenantAccessor));

    public static bool HasName(Secret secret, string name) =>
        string.Equals(secret.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool HasSameTenantName(Secret existing, Secret incoming, bool tenancyEnabled) =>
        (!tenancyEnabled || string.Equals(
            existing.TenantId ?? Tenant.DefaultTenantId,
            incoming.TenantId ?? Tenant.DefaultTenantId,
            StringComparison.Ordinal))
        && HasName(existing, incoming.Name);
}
