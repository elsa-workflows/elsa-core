using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Contracts;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.EntityFrameworkCore.EntityHandlers;

/// <summary>
/// Represents a handler for applying the tenant ID to an entity before saving changes.
/// </summary>
public class ApplyTenantId : IEntitySavingHandler
{
    /// <inheritdoc />
    public ValueTask HandleAsync(ElsaDbContextBase dbContext, EntityEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Entity is Entity entity) 
            entity.TenantId = dbContext.TenantId;

        return default;
    }
}