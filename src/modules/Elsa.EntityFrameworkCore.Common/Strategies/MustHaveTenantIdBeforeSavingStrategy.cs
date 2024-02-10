using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Common.Exceptions;
using Elsa.Tenants.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.EntityFrameworkCore.Common.Strategies;

/// <summary>
/// Ensures that entities of type <see cref="Entity"/> have a TenantId before saving.
/// </summary>
public class MustHaveTenantIdBeforeSavingStrategy(ITenantAccessor tenantAccessor) : IBeforeSavingDbContextStrategy
{
    /// <inheritdoc />
    public bool CanExecute(EntityEntry entityEntry)
    {
        var states = new List<EntityState>
        {
            EntityState.Added
        };

        return entityEntry?.Entity is Entity && states.Contains(entityEntry.State);
    }

    /// <inheritdoc />
    public void Execute(EntityEntry entityEntry)
    {
        string? tenantId = tenantAccessor.GetCurrentTenantId();

        if (tenantId is null)
            throw new MustHaveTenantException($"Entity of type '{entityEntry.Entity.GetType()}' must have a TenantId to be created");

        var entity = (Entity)entityEntry.Entity;
        entity.TenantId = tenantId;
    }
}
