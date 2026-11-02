using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Runs the tenant-agnostic recovery scan and, in addition, one tenant-scoped scan for every tenant whose instances live in its own database.
/// </summary>
internal static class OwnDatabaseTenantRecovery
{
    // enumerate queries the given store; its flag is the filter's TenantAgnostic value.
    // Pass seen to dedupe against an earlier call of the same run. With stampUpdatedAt, each instance's UpdatedAt is set to it
    // in the store it was found in right before its restart, so a scan with an UpdatedAt cutoff skips it while the restart is in flight.
    public static async Task<int> RequeueAsync(
        Func<IWorkflowInstanceStore, bool, IAsyncEnumerable<WorkflowInstanceSummary>> enumerate,
        IWorkflowInstanceStore store,
        IWorkflowRestarter restarter,
        ITenantService? tenantService,
        ITenantAccessor? tenantAccessor,
        ITenantScopeFactory? tenantScopeFactory,
        ILogger logger,
        CancellationToken cancellationToken,
        ISet<(string TenantKey, string Id)>? seen = null,
        DateTimeOffset? stampUpdatedAt = null)
    {
        var ownDatabaseTenants = await OwnDatabaseTenants.TryListAsync(tenantService, tenantScopeFactory, logger, cancellationToken);
        seen ??= new HashSet<(string TenantKey, string Id)>();
        var requeued = 0;

        try
        {
            requeued = await RequeueSharedAsync(enumerate, store, restarter, tenantService, tenantAccessor, tenantScopeFactory, ownDatabaseTenants, seen, stampUpdatedAt, logger, cancellationToken);
        }
        catch (Exception ex) when (OwnDatabaseTenants.IsRecoverable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Failed to recover interrupted workflows from the shared database; tenants with their own database are still scanned.");
        }

        if (ownDatabaseTenants.Count == 0)
            return requeued;

        logger.LogInformation("Recovering interrupted workflows separately for {Count} tenant(s) with their own database.", ownDatabaseTenants.Count);
        logger.LogDebug("Tenants with their own database: {TenantIds}", string.Join(", ", ownDatabaseTenants.Select(x => x.Id)));

        foreach (var tenant in ownDatabaseTenants)
        {
            try
            {
                await using var scope = tenantScopeFactory!.CreateScope(tenant);
                var tenantStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
                var tenantRestarter = Stamping(scope.ServiceProvider.GetRequiredService<IWorkflowRestarter>(), tenantStore, stampUpdatedAt);

                // No tenant lookup per row: rows in this database restart under the scope's tenant.
                var count = await InterruptedWorkflowRequeuer.RequeueAsync(enumerate(tenantStore, false), tenantRestarter, null, null, logger, seen, cancellationToken);
                logger.LogDebug("Requeued {Count} interrupted workflow(s) for tenant {TenantId} from its own database.", count, tenant.Id);
                requeued += count;
            }
            catch (Exception ex) when (OwnDatabaseTenants.IsRecoverable(ex, cancellationToken))
            {
                logger.LogWarning(ex, "Failed to recover interrupted workflows for tenant {TenantId}; it will be retried by the next scan.", tenant.Id);
            }
        }

        return requeued;
    }

    private static async Task<int> RequeueSharedAsync(
        Func<IWorkflowInstanceStore, bool, IAsyncEnumerable<WorkflowInstanceSummary>> enumerate,
        IWorkflowInstanceStore store,
        IWorkflowRestarter restarter,
        ITenantService? tenantService,
        ITenantAccessor? tenantAccessor,
        ITenantScopeFactory? tenantScopeFactory,
        List<Tenant> ownDatabaseTenants,
        ISet<(string TenantKey, string Id)> seen,
        DateTimeOffset? stampUpdatedAt,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var sharedScope = OwnDatabaseTenants.NeedsSharedScope(ownDatabaseTenants, tenantAccessor) ? tenantScopeFactory!.CreateScope(Tenant.Default) : null;
        var sharedStore = sharedScope?.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>() ?? store;
        var sharedRestarter = Stamping(sharedScope?.ServiceProvider.GetRequiredService<IWorkflowRestarter>() ?? restarter, sharedStore, stampUpdatedAt);

        return await InterruptedWorkflowRequeuer.RequeueAsync(enumerate(sharedStore, true), sharedRestarter, tenantService, tenantAccessor, logger, seen, cancellationToken);
    }

    private static IWorkflowRestarter Stamping(IWorkflowRestarter restarter, IWorkflowInstanceStore store, DateTimeOffset? stampUpdatedAt) =>
        stampUpdatedAt is { } value ? new StampingWorkflowRestarter(restarter, store, value) : restarter;

    // Runs inside the requeuer's tenant context, so the stamp is written under the instance's tenant.
    private sealed class StampingWorkflowRestarter(IWorkflowRestarter inner, IWorkflowInstanceStore store, DateTimeOffset updatedAt) : IWorkflowRestarter
    {
        public async Task RestartWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
        {
            await store.UpdateUpdatedTimestampAsync(workflowInstanceId, updatedAt, cancellationToken);
            await inner.RestartWorkflowAsync(workflowInstanceId, cancellationToken);
        }
    }
}
