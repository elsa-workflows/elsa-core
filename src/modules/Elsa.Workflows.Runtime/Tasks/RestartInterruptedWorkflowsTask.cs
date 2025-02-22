using Elsa.Common;
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
            await workflowRestarter.RestartWorkflowAsync(workflowInstance.Id, cancellationToken: cancellationToken);
        }
        logger.LogInformation("Finished restarting interrupted workflows.");
    }

    private WorkflowInstanceFilter CreateWorkflowInstanceFilter()
    {
        var livenessThreshold = options.Value.WorkflowLivenessThreshold;
        var now = systemClock.UtcNow;
        var cutoffTimestamp = now - livenessThreshold;
        return new()
        {
            WorkflowSubStatus = WorkflowSubStatus.Executing,
            BeforeLastUpdated = cutoffTimestamp
        };
    }
}