using Elsa.Common.Multitenancy;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Scheduling.Quartz.Jobs;

/// <summary>
/// A job that executes a background activity.
/// Tenant context is established by the Quartz job factory. The tenant accessor and tenant finder
/// constructor parameters are unused and kept for binary compatibility.
/// </summary>
[UsedImplicitly]
public class ExecuteBackgroundActivityJob(
    IBackgroundActivityInvoker backgroundActivityInvoker,
    ITenantFinder tenantFinder,
    ITenantAccessor tenantAccessor,
    ILogger<ExecuteBackgroundActivityJob> logger) : IJob
{
    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        string? workflowInstanceId = null;

        try
        {
            var map = context.MergedJobDataMap;
            workflowInstanceId = (string?)map.Get(nameof(ScheduledBackgroundActivity.WorkflowInstanceId));
            var scheduledBackgroundActivity = new ScheduledBackgroundActivity(
                workflowInstanceId!,
                (string)map.Get(nameof(ScheduledBackgroundActivity.ActivityNodeId)),
                (string)map.Get(nameof(ScheduledBackgroundActivity.BookmarkId)));

            await backgroundActivityInvoker.ExecuteAsync(scheduledBackgroundActivity, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while executing a background activity for workflow instance {WorkflowInstanceId}", workflowInstanceId);
            throw;
        }
        finally
        {
            await context.DeleteJob(context.JobDetail.Key, cancellationToken);
        }
    }
}
