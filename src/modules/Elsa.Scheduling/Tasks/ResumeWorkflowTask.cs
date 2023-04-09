using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
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
        var workflowDispatcher = context.ServiceProvider.GetRequiredService<IWorkflowDispatcher>();
        await workflowDispatcher.DispatchAsync(_dispatchWorkflowInstanceRequest, context.CancellationToken);
    }
}