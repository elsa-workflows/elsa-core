using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Scheduling.Quartz.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class ResumeWorkflowJob(
    IWorkflowRuntime workflowRuntime,
    IJsonSerializer jsonSerializer,
    ITenantFinder tenantFinder,
    ITenantAccessor tenantAccessor,
    IQuartzJobRetryScheduler retryScheduler,
    ILogger<ResumeWorkflowJob> logger,
    IQuartzScheduleCoordinator? scheduleCoordinator = null) : IJob
{
    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        string? workflowInstanceId = null;
        try
        {
            var tenant = await context.GetTenantAsync(tenantFinder);
            using (tenantAccessor.PushContext(tenant))
            {
                var map = context.MergedJobDataMap;
                var serializedActivityHandle = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.ActivityHandle));
                var activityHandle = serializedActivityHandle != null! ? jsonSerializer.Deserialize<ActivityHandle>(serializedActivityHandle) : null;
                workflowInstanceId = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.WorkflowInstanceId));

                var workflowClient = await workflowRuntime.CreateClientAsync(workflowInstanceId, cancellationToken);
                var request = new RunWorkflowInstanceRequest
                {
                    BookmarkId = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.BookmarkId)),
                    ActivityHandle = activityHandle,
                    Input = map.GetDictionary(nameof(ScheduleExistingWorkflowInstanceRequest.Input)),
                    Properties = map.GetDictionary(nameof(ScheduleExistingWorkflowInstanceRequest.Properties)),
                };
                await workflowClient.RunInstanceAsync(request, cancellationToken: cancellationToken);

                logger.LogInformation("Resumed workflow instance {WorkflowInstanceId}", workflowInstanceId);
            }
        }
        catch (WorkflowGraphNotFoundException e)
        {
            logger.LogWarning(e, "Could not find workflow graph while resuming workflow instance {WorkflowInstanceId}", workflowInstanceId);
            await context.UnscheduleAfterWorkflowGraphNotFoundAsync(scheduleCoordinator, cancellationToken);
        }
        catch (Exception e) when (retryScheduler.IsRetryable(e))
        {
            if (await retryScheduler.TryScheduleRetryAsync(context, e, cancellationToken))
                return;

            logger.LogError(
                e,
                "No retry was scheduled for job {JobKey} after {RetryAttempts} retry attempt(s) (retries disabled or exhausted). Giving up on resuming workflow instance {WorkflowInstanceId}",
                context.JobDetail.Key,
                context.GetRetryAttempt(),
                workflowInstanceId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while resuming workflow instance {WorkflowInstanceId}", workflowInstanceId);
            await context.DeleteJob(context.JobDetail.Key, cancellationToken);
        }
    }
}
