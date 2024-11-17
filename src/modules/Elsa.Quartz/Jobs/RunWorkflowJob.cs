using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Scheduling;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that runs a workflow.
/// </summary>
public class RunWorkflowJob(
    ITenantAccessor tenantAccessor,
    ITenantFinder tenantFinder,
    IWorkflowStarter workflowStarter,
    ILogger<RunWorkflowJob> logger) : IJob
{
    /// <summary>
    /// The job key.
    /// </summary>
    public static readonly JobKey JobKey = new(nameof(RunWorkflowJob));

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        tenantAccessor.Tenant = await context.GetTenantAsync(tenantFinder);
        var map = context.MergedJobDataMap;
        var cancellationToken = context.CancellationToken;

        var startRequest = new StartWorkflowRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId((string)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.WorkflowDefinitionHandle.DefinitionVersionId))),
            CorrelationId = (string?)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.CorrelationId)),
            TriggerActivityId = (string?)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.TriggerActivityId)),
            Input = map.GetDictionary(nameof(ScheduleNewWorkflowInstanceRequest.Input)),
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