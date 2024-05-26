using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Tasks;

/// <summary>
/// A task that resumes a workflow.
/// </summary>
public class ResumeWorkflowTask : ITask
{
    private readonly ScheduleExistingWorkflowInstanceRequest _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResumeWorkflowTask"/> class.
    /// </summary>
    /// <param name="request">The dispatch workflow instance request.</param>
    public ResumeWorkflowTask(ScheduleExistingWorkflowInstanceRequest request)
    {
        _request = request;
    }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(TaskExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var workflowRuntime = context.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync(_request.WorkflowInstanceId, cancellationToken);
        var request = new RunWorkflowInstanceRequest
        {
            Input = _request.Input,
            Properties = _request.Properties,
            ActivityHandle = _request.ActivityHandle,
            BookmarkId = _request.BookmarkId
        };
        await workflowClient.RunInstanceAsync(request, cancellationToken);
    }
}