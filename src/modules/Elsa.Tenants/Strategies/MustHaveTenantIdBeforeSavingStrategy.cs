using Elsa.Common.Contracts;
using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.Tenants.Strategies;

public class MustHaveTenantIdBeforeSavingStrategy : IBeforeSavingDbContextStrategy
{
    private readonly ITenantAccessor _tenantAccessor;

    public MustHaveTenantIdBeforeSavingStrategy(ITenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    public async Task<bool> CanExecute(EntityEntry entityEntry)
    {
        var states = new List<EntityState>() { EntityState.Added };

        return await _tenantAccessor.GetCurrentTenantIdAsync() != null &&
            entityEntry?.Entity is Entity &&
            states.Contains(entityEntry.State);
    }

    public async Task Execute(EntityEntry entityEntry)
    {
        var entity = (Entity)entityEntry.Entity;
        entity.TenantId = await _tenantAccessor.GetCurrentTenantIdAsync();
    }
}
