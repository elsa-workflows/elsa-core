using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Tasks;

/// <summary>
/// A task that resumes a workflow.
/// </summary>
public class ResumeWorkflowTask : ITask
{
    private readonly DispatchWorkflowInstanceRequest _dispatchWorkflowInstanceRequest;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResumeWorkflowTask"/> class.
    /// </summary>
    /// <param name="dispatchWorkflowInstanceRequest">The dispatch workflow instance request.</param>
    public ResumeWorkflowTask(DispatchWorkflowInstanceRequest dispatchWorkflowInstanceRequest)
    {
        _dispatchWorkflowInstanceRequest = dispatchWorkflowInstanceRequest;
    }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(TaskExecutionContext context)
    {
        var workflowRuntime = context.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowInstanceId = _dispatchWorkflowInstanceRequest.InstanceId;
        var options = new ResumeWorkflowRuntimeParams
        {
            CancellationTokens = context.CancellationToken,
            Input = _dispatchWorkflowInstanceRequest.Input,
            ActivityHash = _dispatchWorkflowInstanceRequest.ActivityHash,
            ActivityId = _dispatchWorkflowInstanceRequest.ActivityId,
            Properties = _dispatchWorkflowInstanceRequest.Properties,
            CorrelationId = _dispatchWorkflowInstanceRequest.CorrelationId,
            BookmarkId = _dispatchWorkflowInstanceRequest.BookmarkId,
            ActivityInstanceId = _dispatchWorkflowInstanceRequest.ActivityInstanceId,
            ActivityNodeId = _dispatchWorkflowInstanceRequest.ActivityNodeId,
        };
        await workflowRuntime.ResumeWorkflowAsync(workflowInstanceId, options);
    }
}