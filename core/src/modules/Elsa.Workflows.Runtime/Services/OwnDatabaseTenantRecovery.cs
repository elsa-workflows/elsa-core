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
    // Runtime does not reference Elsa.Persistence.EFCore.Common, which owns this section name.
    private const string ConnectionStringsSectionName = "ConnectionStrings";

    public static bool HasOwnDatabase(this Tenant tenant) =>
        tenant.Configuration.GetSection(ConnectionStringsSectionName).GetChildren().Any(x => !string.IsNullOrWhiteSpace(x.Value));

    // enumerate queries the given store; its flag is the filter's TenantAgnostic value.
    public static async Task<int> RequeueAsync(
        Func<IWorkflowInstanceStore, bool, IAsyncEnumerable<WorkflowInstanceSummary>> enumerate,
        IWorkflowInstanceStore store,
        IWorkflowRestarter restarter,
        ITenantService? tenantService,
        ITenantAccessor? tenantAccessor,
        ITenantScopeFactory? tenantScopeFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var ownDatabaseTenants = await TryListOwnDatabaseTenantsAsync(tenantService, tenantScopeFactory, logger, cancellationToken);
        var seen = new HashSet<string>();
        var requeued = 0;

        try
        {
            requeued = await RequeueSharedAsync(enumerate, store, restarter, tenantService, tenantAccessor, tenantScopeFactory, ownDatabaseTenants, seen, logger, cancellationToken);
        }
        catch (Exception ex) when (IsRecoverable(ex, cancellationToken))
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
                var tenantRestarter = scope.ServiceProvider.GetRequiredService<IWorkflowRestarter>();

                // No tenant lookup per row: rows in this database restart under the scope's tenant.
                var count = await InterruptedWorkflowRequeuer.RequeueAsync(enumerate(tenantStore, false), tenantRestarter, null, null, logger, seen, cancellationToken);
                logger.LogDebug("Requeued {Count} interrupted workflow(s) for tenant {TenantId} from its own database.", count, tenant.Id);
                requeued += count;
            }
            catch (Exception ex) when (IsRecoverable(ex, cancellationToken))
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
        ISet<string> seen,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // RunOnceForAllTenants may run under an own-database tenant, whose scoped store would not see the shared database.
        // Tenant.Default carries no configuration, so its scope resolves the shared database.
        var ambientHasOwnDatabase = ownDatabaseTenants.Count > 0 && tenantAccessor?.Tenant?.HasOwnDatabase() == true;
        await using var sharedScope = ambientHasOwnDatabase ? tenantScopeFactory!.CreateScope(Tenant.Default) : null;
        var sharedStore = sharedScope?.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>() ?? store;
        var sharedRestarter = sharedScope?.ServiceProvider.GetRequiredService<IWorkflowRestarter>() ?? restarter;

        return await InterruptedWorkflowRequeuer.RequeueAsync(enumerate(sharedStore, true), sharedRestarter, tenantService, tenantAccessor, logger, seen, cancellationToken);
    }

    private static async Task<List<Tenant>> TryListOwnDatabaseTenantsAsync(ITenantService? tenantService, ITenantScopeFactory? tenantScopeFactory, ILogger logger, CancellationToken cancellationToken)
    {
        if (tenantService is null || tenantScopeFactory is null)
            return [];

        try
        {
            var tenants = await tenantService.ListAsync(cancellationToken);
            return tenants.Where(x => x.HasOwnDatabase()).ToList();
        }
        catch (Exception ex) when (IsRecoverable(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Failed to list tenants; tenants with their own database are not scanned separately in this run.");
            return [];
        }
    }

    private static bool IsRecoverable(Exception ex, CancellationToken cancellationToken) =>
        !ex.IsFatal() && !(ex is OperationCanceledException && cancellationToken.IsCancellationRequested);
}
