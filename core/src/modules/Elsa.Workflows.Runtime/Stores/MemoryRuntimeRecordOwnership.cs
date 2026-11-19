using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;

namespace Elsa.Workflows.Runtime.Stores;

internal static class MemoryRuntimeRecordOwnership
{
    public static string? GetWriteTenant(Entity record, ITenantAccessor? tenantAccessor)
    {
        if (tenantAccessor == null)
            return record.TenantId;

        var tenant = tenantAccessor.TenantId;
        if (record.TenantId != null && record.TenantId != tenant)
            throw new InvalidOperationException("The record belongs to a different tenant.");

        return tenant;
    }

    public static void DemandOwner(Entity? record, ITenantAccessor? tenantAccessor)
    {
        if (record != null && tenantAccessor != null && !TenantVisibility.CanReplace(record.TenantId, tenantAccessor.TenantId))
            throw new InvalidOperationException("The record belongs to a different tenant.");
    }

    public static IQueryable<T> Visible<T>(IQueryable<T> records, ITenantAccessor? tenantAccessor, bool tenantAgnostic = false)
        where T : Entity =>
        tenantAccessor == null ? records : records.WhereVisibleToTenant(tenantAccessor.TenantId, tenantAgnostic);

    public static IQueryable<T> Owned<T>(IQueryable<T> records, ITenantAccessor? tenantAccessor)
        where T : Entity
    {
        if (tenantAccessor == null)
            return records;

        var tenant = tenantAccessor.TenantId;
        return records.Where(record => record.TenantId == tenant || record.TenantId == null && tenant == Tenant.DefaultTenantId);
    }
}
