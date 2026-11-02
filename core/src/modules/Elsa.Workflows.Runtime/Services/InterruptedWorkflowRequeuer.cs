using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Restarts workflow instances found by a tenant-agnostic query, each one under its own tenant.
/// </summary>
internal static class InterruptedWorkflowRequeuer
{
    /// <summary>
    /// Collects first (restarting shrinks the filtered set, so paging would skip rows), then restarts each instance not yet in <paramref name="seen"/> once under its tenant; returns the count restarted. Share <paramref name="seen"/> across the scans of one run to dedupe them.
    /// It is keyed by tenant and instance id, because separate tenant databases can hold the same instance id.
    /// </summary>
    public static async Task<int> RequeueAsync(
        IAsyncEnumerable<WorkflowInstanceSummary> instances,
        IWorkflowRestarter restarter,
        ITenantService? tenantService,
        ITenantAccessor? tenantAccessor,
        ILogger logger,
        ISet<(string TenantKey, string Id)>? seen,
        CancellationToken cancellationToken)
    {
        var collected = new List<(string Id, string? TenantId)>();
        seen ??= new HashSet<(string TenantKey, string Id)>();
        var tenants = new Dictionary<string, Tenant?>();
        var requeued = 0;

        await foreach (var summary in instances.WithCancellation(cancellationToken))
        {
            if (seen.Add((TenantKey.From(summary.TenantId), summary.Id)))
                collected.Add((summary.Id, summary.TenantId));
        }

        foreach (var (instanceId, instanceTenantId) in collected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (tenantService is not null && tenantAccessor is not null)
                {
                    // A tenant-agnostic instance restarts under "*" when the host registers it: its rows are shared, and the store refuses to replace them under any other tenant.
                    var tenantKey = instanceTenantId == Tenant.AgnosticTenantId ? Tenant.AgnosticTenantId : TenantKey.From(instanceTenantId);

                    if (!tenants.TryGetValue(tenantKey, out var tenant))
                    {
                        tenant = await FindTenantAsync(tenantService, tenants, tenantKey, cancellationToken);

                        if (tenant is null)
                            logger.LogWarning("Skipping {Count} interrupted workflow(s): their tenant {TenantId} was not found.", collected.Count(x => TenantKey.From(x.TenantId) == tenantKey), tenantKey);
                    }

                    if (tenant is null)
                        continue;

                    using (tenantAccessor.PushContext(tenant))
                        await restarter.RestartWorkflowAsync(instanceId, cancellationToken);
                }
                else
                {
                    await restarter.RestartWorkflowAsync(instanceId, cancellationToken);
                }

                requeued++;
            }
            catch (Exception ex) when (!ex.IsFatal() && !(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                logger.LogError(ex, "Failed to requeue interrupted workflow {WorkflowInstanceId}; it will be picked up by a later scan.", instanceId);
            }
        }

        return requeued;
    }

    // Without a registered "*" tenant, tenant-agnostic instances restart under the default tenant.
    private static async Task<Tenant?> FindTenantAsync(ITenantService tenantService, Dictionary<string, Tenant?> tenants, string tenantKey, CancellationToken cancellationToken)
    {
        if (tenants.TryGetValue(tenantKey, out var cached))
            return cached;

        var tenant = await tenantService.FindAsync(tenantKey, cancellationToken);

        if (tenant is null && tenantKey == Tenant.AgnosticTenantId)
            tenant = await FindTenantAsync(tenantService, tenants, Tenant.DefaultTenantId, cancellationToken);

        if (tenant is null && tenantKey == Tenant.DefaultTenantId)
            tenant = Tenant.Default;

        tenants[tenantKey] = tenant;
        return tenant;
    }
}
