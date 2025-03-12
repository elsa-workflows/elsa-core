using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Tasks;

/// <summary>
/// A task that runs a workflow.
/// </summary>
public class RunWorkflowTask : ITask
{
    private readonly DispatchWorkflowDefinitionRequest _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunWorkflowTask"/> class.
    /// </summary>
    public RunWorkflowTask(DispatchWorkflowDefinitionRequest request)
    {
        _request = request;
    }
    
    /// <inheritdoc />
    public async ValueTask ExecuteAsync(TaskExecutionContext context)
    {
        var workflowRuntime = context.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var options = new StartWorkflowRuntimeParams
        {
            Input = _request.Input,
            VersionOptions = _request.VersionOptions,
            Properties = _request.Properties,
            CorrelationId = _request.CorrelationId,
            InstanceId = _request.InstanceId,
            ParentWorkflowInstanceId = _request.ParentWorkflowInstanceId,
            TriggerActivityId = _request.TriggerActivityId,
            CancellationTokens = context.CancellationToken,
        };
        await workflowRuntime.TryStartWorkflowAsync(_request.DefinitionId, options);
    }
}