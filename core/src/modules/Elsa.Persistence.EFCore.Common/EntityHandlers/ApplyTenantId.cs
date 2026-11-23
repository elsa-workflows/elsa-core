using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Tenants.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.EFCore.EntityHandlers;

/// <summary>
/// Represents a handler for applying the tenant ID to an entity before saving changes.
/// </summary>
public class ApplyTenantId(IOptions<TenantsOptions> tenantsOptions) : IEntitySavingHandler
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(ElsaDbContextBase dbContext, EntityEntry entry, CancellationToken cancellationToken = default)
    {
        // Only apply tenant ID if multitenancy is enabled.
        if (!tenantsOptions.Value.IsEnabled)
        {
            return;
        }

        if (entry.Entity is not Entity entity || entity is Tenant)
        {
            return;
        }

        var tenantId = dbContext.TenantId.NormalizeTenantId();
        var canWriteShared = SharedTenantWriteScope.IsGranted(entry.Metadata.ClrType);

        // Stamp a missing tenant first, so a record stamped with the shared tenant is checked like any other shared record.
        if (string.IsNullOrEmpty(entity.TenantId))
            entity.TenantId = tenantId;

        if (entity.TenantId == Tenant.AgnosticTenantId)
        {
            if (!canWriteShared)
                throw new InvalidOperationException("Writing a shared record requires trusted host authority.");
        }
        else if (dbContext.HasTenantContext && entity.TenantId != tenantId)
        {
            throw new InvalidOperationException("The record belongs to a different tenant.");
        }

        if (entry.State != EntityState.Modified)
            return;

        var tenantProperty = entry.Property(nameof(Entity.TenantId));
        var originalTenant = (string?)tenantProperty.OriginalValue;
        if (tenantProperty.Metadata.IsConcurrencyToken && !string.IsNullOrEmpty(originalTenant) && originalTenant == entity.TenantId)
            return;

        // Default-tenant rows can store NULL or an empty string. A detached snapshot does
        // not distinguish them, but the EF concurrency predicate needs the stored value.
        var persisted = await entry.GetDatabaseValuesAsync(cancellationToken);
        if (persisted == null)
            return;

        var originalTenantId = persisted.GetValue<string?>(nameof(Entity.TenantId));
        if (originalTenantId == Tenant.AgnosticTenantId && !canWriteShared)
            throw new InvalidOperationException("Writing a shared record requires trusted host authority.");

        if (originalTenantId.NormalizeTenantId() != entity.TenantId.NormalizeTenantId())
            throw new InvalidOperationException("The record belongs to a different tenant.");

        tenantProperty.OriginalValue = originalTenantId;
    }
}
