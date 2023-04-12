using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
using Quartz;

namespace Elsa.Quartz.Jobs;

/// <summary>
/// A job that runs a workflow.
/// </summary>
public class RunWorkflowJob : IJob
{
    private readonly IWorkflowDispatcher _workflowDispatcher;

    /// <summary>
    /// The job key.
    /// </summary>
    public static readonly JobKey JobKey = new(nameof(RunWorkflowJob));

    /// <summary>
    /// Initializes a new instance of the <see cref="RunWorkflowJob"/> class.
    /// </summary>
    public RunWorkflowJob(IWorkflowDispatcher workflowDispatcher)
    {
        _workflowDispatcher = workflowDispatcher;
    }

    /// <summary>
    /// The workflow request.
    /// </summary>
    public DispatchWorkflowDefinitionRequest Request { get; set; } = default!;

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var request = new DispatchWorkflowDefinitionRequest
        {
            DefinitionId = (string)map.Get(nameof(DispatchWorkflowDefinitionRequest.DefinitionId)),
            VersionOptions = VersionOptions.FromString((string)map.Get(nameof(DispatchWorkflowDefinitionRequest.VersionOptions))),
            TriggerActivityId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.TriggerActivityId)),
            InstanceId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.InstanceId)),
            CorrelationId = (string?)map.Get(nameof(DispatchWorkflowDefinitionRequest.CorrelationId)),
            Input = map.GetDictionary(nameof(DispatchWorkflowDefinitionRequest.Input))
        };
        await _workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}