using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Requests;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that runs a workflow.
/// </summary>
public class RunWorkflowJob(IWorkflowRuntime workflowRuntime) : IJob
{
    /// <summary>
    /// The job key.
    /// </summary>
    public static readonly JobKey JobKey = new(nameof(RunWorkflowJob));

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var definitionId = (string)map.Get(nameof(DispatchWorkflowDefinitionRequest.DefinitionId));
        var options = new StartWorkflowRuntimeParams
        {
            VersionOptions = VersionOptions.FromString((string)map.Get(nameof(DispatchWorkflowDefinitionRequest.VersionOptions))),
            TriggerActivityId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.TriggerActivityId)),
            InstanceId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.InstanceId)),
            CorrelationId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.CorrelationId)),
            Input = map.GetDictionary(nameof(DispatchWorkflowDefinitionRequest.Input))
        };
        await workflowRuntime.TryStartWorkflowAsync(definitionId, options);
    }
}