using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Hangfire.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class RunWorkflowJob
{
    private readonly IWorkflowDispatcher _workflowDispatcher;
        
    /// <summary>
    /// Initializes a new instance of the <see cref="RunWorkflowJob"/> class.
    /// </summary>
    public RunWorkflowJob(IWorkflowDispatcher workflowDispatcher)
    {
        _workflowDispatcher = workflowDispatcher;
    }

    /// <summary>
    /// Executes the job.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="request">The workflow request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExecuteAsync(string name, DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken) => await _workflowDispatcher.DispatchAsync(request, cancellationToken);
}