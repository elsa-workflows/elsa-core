using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Applies tenant ownership to operations that bypass EF Core tracked writes.
/// </summary>
public static class TenantWriteQueryExtensions
{
    /// <summary>
    /// Restricts a write to the ambient tenant or a shared entity for which the host holds write authority.
    /// </summary>
    public static IQueryable<TEntity> WhereTenantCanWrite<TEntity>(this IQueryable<TEntity> query, DbContext dbContext) where TEntity : class
    {
        if (dbContext is not ElsaDbContextBase { IsTenantFilteringEnabled: true } context ||
            !typeof(Entity).IsAssignableFrom(typeof(TEntity)) || typeof(Tenant).IsAssignableFrom(typeof(TEntity)))
            return query;

        var tenantId = context.TenantId.NormalizeTenantId();
        var canWriteShared = SharedTenantWriteScope.IsGranted(typeof(TEntity));

        return query.Where(entity =>
            (canWriteShared && EF.Property<string?>(entity, nameof(Entity.TenantId)) == Tenant.AgnosticTenantId) ||
            (EF.Property<string?>(entity, nameof(Entity.TenantId)) != Tenant.AgnosticTenantId &&
             (EF.Property<string?>(entity, nameof(Entity.TenantId)) == tenantId ||
              EF.Property<string?>(entity, nameof(Entity.TenantId)) == null && tenantId == Tenant.DefaultTenantId)));
    }
}
