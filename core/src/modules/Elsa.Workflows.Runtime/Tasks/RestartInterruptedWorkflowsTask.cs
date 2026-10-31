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
/// A tenant whose instances live in its own database (a connection string in its configuration) is scanned separately in its own scope.
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
    ITenantService? tenantService,
    ITenantAccessor? tenantAccessor,
    ITenantScopeFactory? tenantScopeFactory = null) : RecurringTask
{
    [Obsolete("Use the constructor that accepts an ITenantScopeFactory instead. Without it, tenants with their own database are not recovered. Removed in the next major version.")]
    public RestartInterruptedWorkflowsTask(
        IWorkflowRestarter workflowRestarter,
        IWorkflowInstanceStore workflowInstanceStore,
        ILogger<RestartInterruptedWorkflowsTask> logger,
        IOptions<RuntimeOptions> options,
        ISystemClock systemClock,
        ITenantService? tenantService = null,
        ITenantAccessor? tenantAccessor = null) : this(workflowRestarter, workflowInstanceStore, logger, options, systemClock, tenantService, tenantAccessor, null)
    {
    }

    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var cutoffTimestamp = systemClock.UtcNow - options.Value.InactivityThreshold;
        var batchSize = options.Value.RestartInterruptedWorkflowsBatchSize;

        logger.LogInformation("Restarting interrupted workflows.");
        var restarted = await OwnDatabaseTenantRecovery.RequeueAsync(
            (store, tenantAgnostic) => store.EnumerateSummariesAsync(CreateWorkflowInstanceFilter(cutoffTimestamp, tenantAgnostic), batchSize, cancellationToken),
            workflowInstanceStore,
            workflowRestarter,
            tenantService,
            tenantAccessor,
            tenantScopeFactory,
            logger,
            cancellationToken);
        logger.LogInformation("Finished restarting interrupted workflows; restarted {Count} instance(s).", restarted);
    }

    private static WorkflowInstanceFilter CreateWorkflowInstanceFilter(DateTimeOffset cutoffTimestamp, bool tenantAgnostic) => new()
    {
        IsExecuting = true,
        BeforeLastUpdated = cutoffTimestamp,
        WorkflowStatus = WorkflowStatus.Running,
        TenantAgnostic = tenantAgnostic,
    };
}
