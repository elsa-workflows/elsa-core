using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.Options;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.EFCore.EntityHandlers;

/// <summary>
/// Represents a handler for applying the tenant ID to an entity before saving changes.
/// </summary>
public class ApplyTenantId(IOptions<TenantsOptions> tenantsOptions) : IEntitySavingHandler
{
    /// <inheritdoc />
    public ValueTask HandleAsync(ElsaDbContextBase dbContext, EntityEntry entry, CancellationToken cancellationToken = default)
    {
        // Only apply tenant ID if multitenancy is enabled.
        if (!tenantsOptions.Value.IsEnabled)
        {
            return default;
        }

        if (entry.Entity is not Entity entity)
        {
            return default;
        }

        // Keep tenant-agnostic entities (marked with "*") untouched.
        if (entity.TenantId == Tenant.AgnosticTenantId)
        {
            return default;
        }

        // Stamp the normalized ambient tenant onto entities without an explicit tenant id.
        if (string.IsNullOrEmpty(entity.TenantId))
        {
            entity.TenantId = dbContext.TenantId;
            return default;
        }

        // Without a real tenant context there is nothing to validate against; keep the explicit tenant id.
        if (!dbContext.HasTenantContext)
        {
            return default;
        }

        // Keep an explicit tenant id that matches the ambient tenant; null and "" are equivalent for the
        // default tenant (mirrors Dapper's SameTenant). Unassigned ids never reach this point.
        if (string.Equals(entity.TenantId, dbContext.TenantId, StringComparison.Ordinal))
        {
            return default;
        }

        // Refuse to save an entity whose explicit tenant id differs from the ambient tenant.
        throw new InvalidOperationException($"The entity tenant '{entity.TenantId}' does not match the current tenant '{dbContext.TenantId}'.");
    }
}
