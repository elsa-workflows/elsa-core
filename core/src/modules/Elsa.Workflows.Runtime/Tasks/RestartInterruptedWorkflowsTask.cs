using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Restarts running workflow instances that have not been updated within the inactivity threshold.
/// </summary>
/// <remarks>
/// Runs once per cycle for all tenants; instances with an empty, null or "*" tenant id restart under the default tenant.
/// Tenants with their own databases are not covered.
/// </remarks>
[RunOnceForAllTenants]
[SingleNodeTask(SingleNodeTaskScope.Host)]
[UsedImplicitly]
public class RestartInterruptedWorkflowsTask(
    IWorkflowRestarter workflowRestarter, 
    IWorkflowInstanceStore workflowInstanceStore,
    ILogger<RestartInterruptedWorkflowsTask> logger,
    IOptions<RuntimeOptions> options, 
    ISystemClock systemClock,
    ITenantService? tenantService = null,
    ITenantAccessor? tenantAccessor = null) : RecurringTask
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var workflowInstanceFilter = CreateWorkflowInstanceFilter();
        var batchSize = options.Value.RestartInterruptedWorkflowsBatchSize;
        var workflowInstances = workflowInstanceStore.EnumerateSummariesAsync(workflowInstanceFilter, batchSize, cancellationToken);

        logger.LogInformation("Restarting interrupted workflows.");
        var restarted = await InterruptedWorkflowRequeuer.RequeueAsync(workflowInstances, workflowRestarter, tenantService, tenantAccessor, logger, cancellationToken);
        logger.LogInformation("Finished restarting interrupted workflows; restarted {Count} instance(s).", restarted);
    }

    private WorkflowInstanceFilter CreateWorkflowInstanceFilter()
    {
        var livenessThreshold = options.Value.InactivityThreshold;
        var now = systemClock.UtcNow;
        var cutoffTimestamp = now - livenessThreshold;
        return new()
        {
            IsExecuting = true,
            BeforeLastUpdated = cutoffTimestamp,
            WorkflowStatus = WorkflowStatus.Running,
            TenantAgnostic = true,
        };
    }
}