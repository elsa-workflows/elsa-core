using Elsa.Extensions;
using Elsa.Scheduling;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that runs a workflow.
/// </summary>
public class RunWorkflowJob(IWorkflowRuntime workflowRuntime) : IJob
{
    /// The job key.
    public static readonly JobKey JobKey = new(nameof(RunWorkflowJob));

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var cancellationToken = context.CancellationToken;
        var workflowClient = await workflowRuntime.CreateClientAsync(cancellationToken);
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId((string)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.WorkflowDefinitionHandle.DefinitionVersionId))),
            TriggerActivityId = (string?)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.TriggerActivityId)),
            CorrelationId = (string?)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.CorrelationId)),
            Input = map.GetDictionary(nameof(ScheduleNewWorkflowInstanceRequest.Input)),
            Properties = map.GetDictionary(nameof(ScheduleNewWorkflowInstanceRequest.Properties)),
            ParentId = (string?)map.Get(nameof(ScheduleNewWorkflowInstanceRequest.ParentId))
        };
        await workflowClient.CreateAndRunInstanceAsync(request, cancellationToken: cancellationToken);
    }
}