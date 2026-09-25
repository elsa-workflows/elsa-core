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
            var updated = await dbContext.WorkflowDefinitionRegistryGenerations
                .Where(x => x.TenantId == normalizedTenantId)
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
                var insertedByAnotherNode = await dbContext.WorkflowDefinitionRegistryGenerations
                    .AnyAsync(x => x.TenantId == normalizedTenantId, cancellationToken);
                if (!insertedByAnotherNode)
                    throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<long> GetGenerationAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        return await dbContext.WorkflowDefinitionRegistryGenerations
            .Where(x => x.TenantId == normalizedTenantId)
            .Select(x => x.Generation)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static string NormalizeTenantId(string? tenantId) => tenantId ?? Tenant.DefaultTenantId;
}
