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
        // Only apply tenant ID if multitenancy is enabled
        if (!tenantsOptions.Value.IsEnabled)
            return default;

        if (entry.Entity is not Entity entity)
            return default;

        // Don't touch tenant-agnostic entities (marked with "*")
        if (entity.TenantId == Tenant.AgnosticTenantId)
            return default;

        // Apply current tenant ID to entities without one
        if (entity.TenantId == null && dbContext.TenantId != null)
            entity.TenantId = dbContext.TenantId;

        return default;
    }
}