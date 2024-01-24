using Elsa.Common.Contracts;
using Elsa.Common.Entities;
using Elsa.EntityFrameworkCore.Common.Abstractions;
using Elsa.EntityFrameworkCore.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Elsa.EntityFrameworkCore.Common.Strategies;

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

        return entityEntry?.Entity is Entity &&
            states.Contains(entityEntry.State);
    }

    public async Task Execute(EntityEntry entityEntry)
    {
        string? tenantId = _tenantAccessor.GetCurrentTenantId();

        if (tenantId is null)
            throw new MustHaveTenantException($"Entity of type '{entityEntry.Entity.GetType()}' must have a TenantId to be created");

        var entity = (Entity)entityEntry.Entity;
        entity.TenantId = tenantId;
    }
}
