using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// Stores workflow-definition registry generations in the shared management database.
/// </summary>
public class EFCoreWorkflowDefinitionRegistryGenerationStore(ManagementElsaDbContext dbContext) : IWorkflowDefinitionRegistryGenerationStore
{
    /// <inheritdoc />
    public bool IsShared => true;

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);

        while (true)
        {
            var updated = await WhereTenant(dbContext.WorkflowDefinitionRegistryGenerations, normalizedTenantId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Generation, x => x.Generation + 1), cancellationToken);

            if (updated > 0)
                return await GetGenerationAsync(normalizedTenantId, cancellationToken);

            var entity = new WorkflowDefinitionRegistryGeneration
            {
                TenantId = normalizedTenantId,
                Generation = 1
            };
            dbContext.WorkflowDefinitionRegistryGenerations.Add(entity);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return 1;
            }
            catch (DbUpdateException exception) when (DbExceptionClassifier.IsDuplicateKey(exception))
            {
                // Another node may have inserted the first generation concurrently.
                dbContext.Entry(entity).State = EntityState.Detached;
                var insertedByAnotherNode = await WhereTenant(dbContext.WorkflowDefinitionRegistryGenerations, normalizedTenantId)
                    .AnyAsync(cancellationToken);
                if (!insertedByAnotherNode)
                    throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<long> GetGenerationAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        return await WhereTenant(dbContext.WorkflowDefinitionRegistryGenerations, normalizedTenantId)
            .Select(x => x.Generation)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static string NormalizeTenantId(string? tenantId) => tenantId ?? Tenant.DefaultTenantId;

    /// <summary>
    /// Oracle stores the default tenant as <c>NULL</c>. ExecuteUpdate parameterizes a captured
    /// <c>""</c>, so <c>TenantId = :p</c> with a null bind matches nothing. A constant
    /// <c>x.TenantId == null</c> becomes <c>IS NULL</c>. Other providers store the default tenant as
    /// <c>""</c> on a NOT NULL key, so <c>|| TenantId == ""</c> keeps those lookups working.
    /// </summary>
    private static IQueryable<WorkflowDefinitionRegistryGeneration> WhereTenant(
        IQueryable<WorkflowDefinitionRegistryGeneration> source,
        string tenantId)
    {
        if (tenantId == Tenant.DefaultTenantId)
            return source.Where(x => x.TenantId == null || x.TenantId == Tenant.DefaultTenantId);

        return source.Where(x => x.TenantId == tenantId);
    }
}
