using Elsa.Common;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elsa.Workflows.Runtime.HealthChecks;

/// <summary>
/// Performs small read-only probes against the workflow management and runtime stores.
/// </summary>
public class ElsaWorkflowPersistenceHealthCheck(
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowInstanceStore workflowInstanceStore,
    ITriggerStore triggerStore,
    IBookmarkQueueStore bookmarkQueueStore) : IHealthCheck
{
    private const string ProbeId = "__elsa_health_check_probe__";

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { Id = ProbeId }, cancellationToken);
            await workflowInstanceStore.CountAsync(new WorkflowInstanceFilter { Id = ProbeId }, cancellationToken);
            await triggerStore.FindAsync(new TriggerFilter { Id = ProbeId }, cancellationToken);
            await bookmarkQueueStore.FindAsync(new BookmarkQueueFilter { Id = ProbeId }, cancellationToken);

            return HealthCheckResult.Healthy("Elsa workflow stores are reachable.", new Dictionary<string, object>
            {
                ["category"] = "persistence",
                ["probes"] = "workflow-definitions,workflow-instances,triggers,bookmark-queue"
            });
        }
        catch (Exception e) when (!e.IsFatal())
        {
            return HealthCheckResult.Unhealthy("Elsa workflow stores are not reachable.", e, new Dictionary<string, object>
            {
                ["category"] = "persistence"
            });
        }
    }
}
