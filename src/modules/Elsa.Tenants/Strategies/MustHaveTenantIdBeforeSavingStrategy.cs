using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.Tenants.Accessors;
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

        return await _tenantAccessor.GetCurrentTenantAsync() != null &&
            entityEntry?.Entity is Entity &&
            states.Contains(entityEntry.State);
    }

    public async Task Execute(EntityEntry entityEntry)
    {
        var entity = (Entity)entityEntry.Entity;
        var tenant = await _tenantAccessor.GetCurrentTenantAsync();
        if (tenant is null)
            return;

        entity.TenantId = tenant.TenantId;
    }
}
