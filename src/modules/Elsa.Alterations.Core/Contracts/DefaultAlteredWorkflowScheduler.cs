using Elsa.Alterations.Core.Results;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Alterations.Core.Contracts;

/// <inheritdoc />
public class DefaultAlteredWorkflowDispatcher : IAlteredWorkflowDispatcher
{
    private readonly IWorkflowDispatcher _workflowDispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlteredWorkflowDispatcher"/> class.
    /// </summary>
    public DefaultAlteredWorkflowDispatcher(IWorkflowDispatcher workflowDispatcher)
    {
        _workflowDispatcher = workflowDispatcher;
    }
    
    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<RunAlterationsResult> results, CancellationToken cancellationToken = default)
    {
        foreach (var result in results.Where(x => x is { IsSuccessful: true, WorkflowHasScheduledWork: true })) 
            await DispatchAsync(result, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchAsync(RunAlterationsResult result, CancellationToken cancellationToken = default)
    {
        await _workflowDispatcher.DispatchAsync(new DispatchWorkflowInstanceRequest(result.WorkflowInstanceId), cancellationToken);
    }
}