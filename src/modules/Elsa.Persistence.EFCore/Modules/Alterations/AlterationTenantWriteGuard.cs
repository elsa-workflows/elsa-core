using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;

namespace Elsa.Persistence.EFCore.Modules.Alterations;

/// <summary>
/// Memory Alteration stores refuse to replace a <c>*</c> row unless the writer is agnostic.
/// EF <c>SaveAsync</c> otherwise treats the visible <c>*</c> row as an upsert target.
/// </summary>
internal static class AlterationTenantWriteGuard
{
    public static async Task EnsureCanReplaceAsync<TEntity>(
        EntityStore<AlterationsElsaDbContext, TEntity> store,
        TEntity entity,
        ITenantAccessor? tenantAccessor,
        string entityName,
        CancellationToken cancellationToken) where TEntity : Entity, new()
    {
        if (tenantAccessor is null)
            return;

        var existing = await store.FindAsync(x => x.Id == entity.Id, cancellationToken);

        if (existing is null)
            return;

        var currentTenantId = tenantAccessor.TenantId ?? Tenant.DefaultTenantId;
        var canReplace = existing.TenantId == Tenant.AgnosticTenantId
            ? currentTenantId == Tenant.AgnosticTenantId
            : TenantVisibility.IsVisible(existing.TenantId, currentTenantId);

        if (!canReplace)
        {
            throw new InvalidOperationException(
                $"An {entityName} with ID '{entity.Id}' already exists and is not visible to the current tenant.");
        }
    }
}
