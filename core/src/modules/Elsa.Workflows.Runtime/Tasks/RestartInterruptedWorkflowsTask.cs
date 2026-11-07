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
/// <para>Each cycle restarts stale executing instances, then stale <see cref="WorkflowSubStatus.Interrupted"/> instances.
/// A drain on another node can interrupt instances after this node's startup scan ran, for example during a slot swap or a rolling deploy.
/// An Interrupted instance gets a fresh <c>UpdatedAt</c> right before its restart, so the next cycle skips it while the restart is in flight.</para>
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
        var seen = new HashSet<(string TenantKey, string Id)>();

        logger.LogInformation("Restarting interrupted workflows.");
        var restarted = await OwnDatabaseTenantRecovery.RequeueAsync(
            (store, tenantAgnostic) => store.EnumerateSummariesAsync(CreateWorkflowInstanceFilter(cutoffTimestamp, tenantAgnostic), batchSize, cancellationToken),
            workflowInstanceStore,
            workflowRestarter,
            tenantService,
            tenantAccessor,
            tenantScopeFactory,
            logger,
            cancellationToken,
            seen);
        restarted += await OwnDatabaseTenantRecovery.RequeueAsync(
            (store, tenantAgnostic) => store.EnumerateSummariesAsync(CreateInterruptedFilter(cutoffTimestamp, tenantAgnostic), batchSize, cancellationToken),
            workflowInstanceStore,
            workflowRestarter,
            tenantService,
            tenantAccessor,
            tenantScopeFactory,
            logger,
            cancellationToken,
            seen,
            stampClock: systemClock);
        logger.LogInformation("Finished restarting interrupted workflows; restarted {Count} instance(s).", restarted);
    }

    private static WorkflowInstanceFilter CreateWorkflowInstanceFilter(DateTimeOffset cutoffTimestamp, bool tenantAgnostic) => new()
    {
        IsExecuting = true,
        BeforeLastUpdated = cutoffTimestamp,
        WorkflowStatus = WorkflowStatus.Running,
        TenantAgnostic = tenantAgnostic,
    };

    // The startup scan's filter, limited to instances not updated within the inactivity threshold.
    private static WorkflowInstanceFilter CreateInterruptedFilter(DateTimeOffset cutoffTimestamp, bool tenantAgnostic) => new()
    {
        WorkflowSubStatus = WorkflowSubStatus.Interrupted,
        WorkflowStatus = WorkflowStatus.Running,
        BeforeLastUpdated = cutoffTimestamp,
        TenantAgnostic = tenantAgnostic,
    };
}
