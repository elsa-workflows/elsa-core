using Elsa.Scheduling;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Hangfire.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class ResumeWorkflowJob(IWorkflowRuntime workflowRuntime)
{
    /// <summary>
    /// Executes the job.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="request">The workflow request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExecuteAsync(string name, ScheduleExistingWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        var client = await workflowRuntime.CreateClientAsync(cancellationToken);
        var runRequest = new RunWorkflowInstanceRequest
        {
            BookmarkId = request.BookmarkId,
            ActivityHandle = request.ActivityHandle,
            Input = request.Input,
            Properties = request.Properties
        };
        await client.RunInstanceAsync(runRequest, cancellationToken);
    }
}