using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Scheduling.Quartz.Jobs;

/// <summary>
/// A job that executes a background activity.
/// Tenant context is established by the Quartz job factory when it is registered.
/// When it is not, <paramref name="tenantAccessor"/> and <paramref name="tenantFinder"/> look up
/// and push the trigger's tenant at the start of Execute.
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
            var scheduledTenant = await context.FindScheduledTenantIfMissingAsync(tenantAccessor, tenantFinder, cancellationToken);
            using (tenantAccessor.Tenant is null ? tenantAccessor.PushContext(scheduledTenant) : null)
            {
                var map = context.MergedJobDataMap;
                workflowInstanceId = (string?)map.Get(nameof(ScheduledBackgroundActivity.WorkflowInstanceId));
                var scheduledBackgroundActivity = new ScheduledBackgroundActivity(
                    workflowInstanceId!,
                    (string)map.Get(nameof(ScheduledBackgroundActivity.ActivityNodeId)),
                    (string)map.Get(nameof(ScheduledBackgroundActivity.BookmarkId)));

                await backgroundActivityInvoker.ExecuteAsync(scheduledBackgroundActivity, cancellationToken);
            }
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
