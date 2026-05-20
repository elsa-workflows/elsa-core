using Elsa.Common;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elsa.Workflows.Runtime.HealthChecks;

/// <summary>
/// Performs small read-only probes against the workflow management and runtime stores.
/// </summary>
public class ElsaWorkflowPersistenceHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    private const string ProbeId = "__elsa_health_check_probe__";

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var failedStore = "";
        var probes = new List<string>();
        var skippedProbes = new List<string>();

        try
        {
            await ProbeAsync("workflow-definitions", serviceProvider.GetService<IWorkflowDefinitionStore>(), async (store, ct) => await store.FindAsync(new WorkflowDefinitionFilter { Id = ProbeId }, ct));
            await ProbeAsync("workflow-instances", serviceProvider.GetService<IWorkflowInstanceStore>(), async (store, ct) => await store.CountAsync(new WorkflowInstanceFilter { Id = ProbeId }, ct));
            await ProbeAsync("triggers", serviceProvider.GetService<ITriggerStore>(), async (store, ct) => await store.FindAsync(new TriggerFilter { Id = ProbeId }, ct));
            await ProbeAsync("bookmark-queue", serviceProvider.GetService<IBookmarkQueueStore>(), async (store, ct) => await store.FindAsync(new BookmarkQueueFilter { Id = ProbeId }, ct));

            var data = CreateData();
            return probes.Count == 0
                ? HealthCheckResult.Degraded("No Elsa workflow persistence stores are registered.", data: data)
                : HealthCheckResult.Healthy("Elsa workflow stores are reachable.", data);
        }
        catch (Exception e) when (!e.IsFatal())
        {
            return HealthCheckResult.Unhealthy($"Elsa workflow store '{failedStore}' is not reachable.", e, new Dictionary<string, object>
            {
                ["category"] = "persistence",
                ["failedStore"] = failedStore,
                ["failedProbe"] = failedStore
            });
        }

        async Task ProbeAsync<TStore>(string storeName, TStore? store, Func<TStore, CancellationToken, Task> probe) where TStore : class
        {
            if (store == null)
            {
                skippedProbes.Add(storeName);
                return;
            }

            failedStore = storeName;
            probes.Add(storeName);
            await probe(store, cancellationToken);
        }

        Dictionary<string, object> CreateData()
        {
            var data = new Dictionary<string, object>
            {
                ["category"] = "persistence"
            };

            if (probes.Count > 0)
                data["probes"] = string.Join(",", probes);

            if (skippedProbes.Count > 0)
                data["skippedProbes"] = string.Join(",", skippedProbes);

            return data;
        }
    }
}
