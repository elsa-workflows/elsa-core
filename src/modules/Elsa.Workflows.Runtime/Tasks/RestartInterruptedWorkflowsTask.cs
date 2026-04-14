using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Tasks;

[SingleNodeTask]
[UsedImplicitly]
public class RestartInterruptedWorkflowsTask(
    IWorkflowInstanceStore workflowInstanceStore, 
    ITenantAccessor tenantAccessor,
    ITenantService tenantService,
    IWorkflowRestarter workflowRestarter, 
    IOptions<RuntimeOptions> options, 
    ISystemClock systemClock,
    ILogger<RestartInterruptedWorkflowsTask> logger) : RecurringTask
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var workflowInstanceFilter = CreateWorkflowInstanceFilter();
        var batchSize = options.Value.RestartInterruptedWorkflowsBatchSize;
        var workflowInstances = workflowInstanceStore.EnumerateSummariesAsync(workflowInstanceFilter, batchSize, cancellationToken);

        logger.LogInformation("Restarting interrupted workflows.");
        await foreach (var workflowInstance in workflowInstances)
        {
            try
            {
                var tenantId = workflowInstance.TenantId ?? string.Empty;

                if (string.IsNullOrWhiteSpace(tenantId) || tenantId == Tenant.AgnosticTenantId)
                {
                    await workflowRestarter.RestartWorkflowAsync(workflowInstance.Id, cancellationToken: cancellationToken);
                    continue;
                }

                var tenant = await tenantService.FindAsync(tenantId, cancellationToken) ?? new Tenant { Id = tenantId, Name = tenantId };

                using (tenantAccessor.PushContext(tenant))
                    await workflowRestarter.RestartWorkflowAsync(workflowInstance.Id, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to restart interrupted workflow {WorkflowInstanceId}", workflowInstance.Id);
            }
        }
        logger.LogInformation("Finished restarting interrupted workflows.");
    }

    private WorkflowInstanceFilter CreateWorkflowInstanceFilter()
    {
        var livenessThreshold = options.Value.InactivityThreshold;
        var now = systemClock.UtcNow;
        var cutoffTimestamp = now - livenessThreshold;
        return new()
        {
            IsExecuting = true,
            BeforeLastUpdated = cutoffTimestamp
        };
    }
}