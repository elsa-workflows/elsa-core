using Elsa.Common;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.HealthChecks;

/// <summary>
/// Performs small read-only probes against the workflow management and runtime stores.
/// </summary>
public class ElsaWorkflowPersistenceHealthCheck(IServiceProvider serviceProvider, ILogger<ElsaWorkflowPersistenceHealthCheck> logger) : IHealthCheck
{
    private const string ProbeId = "00000000-0000-0000-0000-000000000000";

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var probeResults = await Task.WhenAll(
            ProbeAsync("workflow-definitions", serviceProvider.GetService<IWorkflowDefinitionStore>(), async (store, ct) => await store.FindAsync(new WorkflowDefinitionFilter { Id = ProbeId }, ct)),
            ProbeAsync("workflow-instances", serviceProvider.GetService<IWorkflowInstanceStore>(), async (store, ct) => await store.CountAsync(new WorkflowInstanceFilter { Id = ProbeId }, ct)),
            ProbeAsync("triggers", serviceProvider.GetService<ITriggerStore>(), async (store, ct) => await store.FindAsync(new TriggerFilter { Id = ProbeId }, ct)),
            ProbeAsync("bookmark-queue", serviceProvider.GetService<IBookmarkQueueStore>(), async (store, ct) => await store.FindAsync(new BookmarkQueueFilter { Id = ProbeId }, ct)));

        var attemptedProbes = probeResults.Where(x => !x.Skipped).Select(x => x.StoreName).ToList();
        var successfulProbes = probeResults.Where(x => !x.Skipped && x.Exception == null).Select(x => x.StoreName).ToList();
        var skippedProbes = probeResults.Where(x => x.Skipped).Select(x => x.StoreName).ToList();
        var failedProbe = probeResults.FirstOrDefault(x => x.Exception != null);
        if (failedProbe != null)
        {
            var data = CreateData();
            data["failedStore"] = failedProbe.StoreName;
            data["failedProbe"] = failedProbe.StoreName;

            logger.LogWarning(failedProbe.Exception, "Elsa workflow store {StoreName} is not reachable.", failedProbe.StoreName);
            return HealthCheckResult.Unhealthy($"Elsa workflow store '{failedProbe.StoreName}' is not reachable.", data: data);
        }

        var healthyData = CreateData();
        return attemptedProbes.Count == 0
            ? HealthCheckResult.Degraded("No Elsa workflow persistence stores are registered.", data: healthyData)
            : HealthCheckResult.Healthy("Elsa workflow stores are reachable.", healthyData);

        async Task<ProbeResult> ProbeAsync<TStore>(string storeName, TStore? store, Func<TStore, CancellationToken, Task> probe) where TStore : class
        {
            if (store == null)
            {
                return new ProbeResult(storeName, true, null);
            }

            try
            {
                await probe(store, cancellationToken);
                return new ProbeResult(storeName, false, null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e) when (!e.IsFatal())
            {
                return new ProbeResult(storeName, false, e);
            }
        }

        Dictionary<string, object> CreateData()
        {
            var data = new Dictionary<string, object>
            {
                ["category"] = "persistence"
            };

            if (successfulProbes.Count > 0)
                data["probes"] = string.Join(",", successfulProbes);

            if (attemptedProbes.Count > 0)
                data["attemptedProbes"] = string.Join(",", attemptedProbes);

            if (skippedProbes.Count > 0)
                data["skippedProbes"] = string.Join(",", skippedProbes);

            return data;
        }
    }

    private sealed record ProbeResult(string StoreName, bool Skipped, Exception? Exception);
}
