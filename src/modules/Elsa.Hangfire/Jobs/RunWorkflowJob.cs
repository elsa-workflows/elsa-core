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
    /// <param name="request">The workflow request.</param>
    public async Task ExecuteAsync(DispatchWorkflowDefinitionRequest request) => await _workflowDispatcher.DispatchAsync(request);
}