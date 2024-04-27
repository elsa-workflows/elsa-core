using Elsa.Extensions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that runs a workflow.
/// </summary>
public class RunWorkflowJob(IWorkflowDispatcher workflowDispatcher) : IJob
{
    /// The job key.
    public static readonly JobKey JobKey = new(nameof(RunWorkflowJob));

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var request = new DispatchWorkflowDefinitionRequest
        {
            DefinitionVersionId = (string)map.Get(nameof(DispatchWorkflowDefinitionRequest.DefinitionVersionId)),
            TriggerActivityId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.TriggerActivityId)),
            InstanceId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.InstanceId)),
            CorrelationId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.CorrelationId)),
            Input = map.GetDictionary(nameof(DispatchWorkflowDefinitionRequest.Input))
        };
        await workflowDispatcher.DispatchAsync(request, cancellationToken: context.CancellationToken);
    }
}