using Elsa.Scheduling;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Hangfire.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class RunWorkflowJob(IWorkflowRuntime workflowRuntime)
{
    /// <summary>
    /// Executes the job.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="request">The workflow request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExecuteAsync(string name, ScheduleNewWorkflowInstanceRequest request, CancellationToken cancellationToken)
    {
        var client = await workflowRuntime.CreateClientAsync(cancellationToken);
        var createAndRunRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = request.WorkflowDefinitionHandle,
            TriggerActivityId = request.TriggerActivityId,
            CorrelationId = request.CorrelationId,
            Input = request.Input,
            Properties = request.Properties,
            ParentId = request.ParentId
        };
        await client.CreateAndRunInstanceAsync(createAndRunRequest, cancellationToken);
    }
}