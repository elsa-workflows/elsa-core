using Elsa.Common.Entities;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Memory-store counterpart of EF Core <c>SetTenantIdFilter</c>.
/// A row is visible when its <see cref="Entity.TenantId"/> matches the ambient tenant,
/// is <see cref="Tenant.AgnosticTenantId"/>, or is null while the ambient tenant is the default.
/// </summary>
public static class TenantVisibility
{
    /// <summary>
    /// Returns whether <paramref name="entityTenantId"/> is visible under <paramref name="ambientTenantId"/>.
    /// </summary>
    public static bool IsVisible(string? entityTenantId, string ambientTenantId) =>
        entityTenantId == ambientTenantId
        || entityTenantId == Tenant.AgnosticTenantId
        || entityTenantId is null && ambientTenantId == Tenant.DefaultTenantId;

    /// <summary>
    /// Restricts <paramref name="queryable"/> to rows visible to <paramref name="ambientTenantId"/>,
    /// unless <paramref name="tenantAgnostic"/> is set (EF <c>IgnoreQueryFilters</c>).
    /// </summary>
    public static IQueryable<T> WhereVisibleToTenant<T>(this IQueryable<T> queryable, string ambientTenantId, bool tenantAgnostic = false)
        where T : Entity
    {
        if (tenantAgnostic)
            return queryable;

        return queryable.Where(entity =>
            entity.TenantId == ambientTenantId
            || entity.TenantId == Tenant.AgnosticTenantId
            || entity.TenantId == null && ambientTenantId == Tenant.DefaultTenantId);
    }
}
