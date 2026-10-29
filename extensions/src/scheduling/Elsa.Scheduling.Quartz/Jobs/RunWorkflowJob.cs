using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Scheduling.Quartz;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Scheduling.Quartz.Jobs;

/// <summary>
/// A job that runs a workflow.
/// Tenant context is established by the Quartz job factory when it is registered.
/// When it is not, <paramref name="tenantAccessor"/> and <paramref name="tenantFinder"/> look up
/// and push the trigger's tenant at the start of Execute.
/// </summary>
[UsedImplicitly]
public class RunWorkflowJob(
    ITenantAccessor tenantAccessor,
    ITenantFinder tenantFinder,
    IWorkflowStarter workflowStarter,
    IQuartzJobRetryScheduler retryScheduler,
    ILogger<RunWorkflowJob> logger,
    IQuartzScheduleCoordinator? scheduleCoordinator = null) : IJob
{
    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        StartWorkflowRequest? startRequest = null;
        try
        {
            var scheduledTenant = await context.FindScheduledTenantIfMissingAsync(tenantAccessor, tenantFinder, cancellationToken);
            using (tenantAccessor.Tenant is null ? tenantAccessor.PushContext(scheduledTenant) : null)
            {
                var map = context.MergedJobDataMap;

                startRequest = new StartWorkflowRequest
                {
                    WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId((string)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.WorkflowDefinitionHandle.DefinitionVersionId))),
                    CorrelationId = (string?)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.CorrelationId)),
                    TriggerActivityId = (string?)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.TriggerActivityId)),
                    Input = map.GetDictionary(nameof(ScheduleNewWorkflowInstanceRequest.Input)),
                    Variables = map.GetDictionary(nameof(ScheduleNewWorkflowInstanceRequest.Variables)),
                    Properties = map.GetDictionary(nameof(ScheduleNewWorkflowInstanceRequest.Properties)),
                    ParentId = (string?)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.ParentId))
                };

                var startResponse = await workflowStarter.StartWorkflowAsync(startRequest, cancellationToken);

                if (startResponse.CannotStart)
                {
                    logger.LogWarning("Workflow activation strategy disallowed starting workflow {WorkflowDefinitionHandle} with correlation ID {CorrelationId}", startRequest.WorkflowDefinitionHandle, startRequest.CorrelationId);
                    return;
                }

                logger.LogInformation("Started workflow {WorkflowInstanceId} with correlation ID {CorrelationId}", startResponse.WorkflowInstanceId, startRequest.CorrelationId);
            }
        }
        catch (WorkflowGraphNotFoundException e)
        {
            logger.LogWarning(e, "Could not find workflow graph for workflow definition handle {WorkflowDefinitionHandle}", startRequest?.WorkflowDefinitionHandle);
            await context.UnscheduleAfterWorkflowGraphNotFoundAsync(scheduleCoordinator, cancellationToken);
        }
        catch (Exception e) when (retryScheduler.IsRetryable(e))
        {
            if (await retryScheduler.TryScheduleRetryAsync(context, e, cancellationToken))
                return;

            logger.LogError(
                e,
                "No retry was scheduled for job {JobKey} after {RetryAttempts} retry attempt(s) (retries disabled or exhausted). Giving up on starting workflow {WorkflowDefinitionHandle} with correlation ID {CorrelationId}",
                context.JobDetail.Key,
                context.GetRetryAttempt(),
                startRequest?.WorkflowDefinitionHandle,
                startRequest?.CorrelationId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while starting workflow {WorkflowDefinitionHandle} with correlation ID {CorrelationId}", startRequest?.WorkflowDefinitionHandle, startRequest?.CorrelationId);
            await context.DeleteJob(context.JobDetail.Key, cancellationToken);
        }
    }
}
