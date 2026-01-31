using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.Persistence.EFCore.EntityHandlers;

/// <summary>
/// Represents a handler for applying the tenant ID to an entity before saving changes.
/// </summary>
public class ApplyTenantId : IEntitySavingHandler
{
    /// <inheritdoc />
    public ValueTask HandleAsync(ElsaDbContextBase dbContext, EntityEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Entity is Entity entity)
        {
            // Don't touch tenant-agnostic entities (marked with "*")
            if (entity.TenantId == Tenant.AgnosticTenantId)
                return default;

            // Apply current tenant ID to entities without one
            if (entity.TenantId == null && dbContext.TenantId != null)
                entity.TenantId = dbContext.TenantId;
        }

        return default;
    }
}