using System.Linq.Expressions;
using Elsa.Alterations.Core.Stores;
using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Alterations;

/// <summary>
/// Compare-and-swap upsert for Alterations EF writes. Ownership is decided in the UPDATE
/// predicate itself (no pre-read guard). A 0-row update falls through to INSERT; a PK
/// collision is the unowned-row refuse path.
/// </summary>
internal static class AlterationTenantOwnedUpsert
{
    public static void StampTenantId(Entity entity, string? ambientTenantId)
    {
        if (entity.TenantId == Tenant.AgnosticTenantId)
            return;

        if (entity.TenantId == null && ambientTenantId != null)
            entity.TenantId = ambientTenantId;
    }

    public static string AmbientTenantId(AlterationsElsaDbContext dbContext) =>
        dbContext.TenantId ?? Tenant.DefaultTenantId;

    /// <summary>
    /// UPDATE is allowed when the existing row's tenant matches the stamped source tenant
    /// (own row), or when both sides are <c>*</c> and the writer is agnostic. A named
    /// writer never matches a <c>*</c> or other-tenant row.
    /// </summary>
    public static Expression<Func<TEntity, bool>> OwnedId<TEntity>(string id, string? sourceTenantId, string ambientTenantId)
        where TEntity : Entity
    {
        return entity => entity.Id == id && (
            (entity.TenantId == sourceTenantId && entity.TenantId != Tenant.AgnosticTenantId)
            || (entity.TenantId == Tenant.AgnosticTenantId && sourceTenantId == Tenant.AgnosticTenantId && ambientTenantId == Tenant.AgnosticTenantId)
            || (entity.TenantId == null && ambientTenantId == Tenant.DefaultTenantId && (sourceTenantId == null || sourceTenantId == Tenant.DefaultTenantId)));
    }

    public static async Task InsertIfAbsentAsync<TEntity>(
        AlterationsElsaDbContext dbContext,
        TEntity entity,
        bool isPlan,
        CancellationToken cancellationToken)
        where TEntity : Entity
    {
        dbContext.Entry(entity).State = EntityState.Added;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DbExceptionClassifier.IsDuplicateKey(exception))
        {
            dbContext.Entry(entity).State = EntityState.Detached;
            throw isPlan
                ? AlterationStoreConflict.HiddenPlanId(entity.Id)
                : AlterationStoreConflict.HiddenJobId(entity.Id);
        }
    }
}
