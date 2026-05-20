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
        var failedStore = "";

        try
        {
            await ProbeAsync("workflow-definitions", async ct => await workflowDefinitionStore.FindAsync(new WorkflowDefinitionFilter { Id = ProbeId }, ct));
            await ProbeAsync("workflow-instances", async ct => await workflowInstanceStore.CountAsync(new WorkflowInstanceFilter { Id = ProbeId }, ct));
            await ProbeAsync("triggers", async ct => await triggerStore.FindAsync(new TriggerFilter { Id = ProbeId }, ct));
            await ProbeAsync("bookmark-queue", async ct => await bookmarkQueueStore.FindAsync(new BookmarkQueueFilter { Id = ProbeId }, ct));

            return HealthCheckResult.Healthy("Elsa workflow stores are reachable.", new Dictionary<string, object>
            {
                ["category"] = "persistence",
                ["probes"] = "workflow-definitions,workflow-instances,triggers,bookmark-queue"
            });
        }
        catch (Exception e) when (!e.IsFatal())
        {
            return HealthCheckResult.Unhealthy($"Elsa workflow store '{failedStore}' is not reachable.", e, new Dictionary<string, object>
            {
                ["category"] = "persistence",
                ["failedStore"] = failedStore
            });
        }

        async Task ProbeAsync(string store, Func<CancellationToken, Task> probe)
        {
            failedStore = store;
            await probe(cancellationToken);
        }
    }
}
