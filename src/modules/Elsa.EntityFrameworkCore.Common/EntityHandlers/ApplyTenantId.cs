using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.EntityFrameworkCore.Common.EntityHandlers;

/// <summary>
/// 
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