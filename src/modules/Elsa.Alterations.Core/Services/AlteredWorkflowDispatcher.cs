using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Results;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
/// <summary>
/// Initializes a new instance of the <see cref="AlteredWorkflowDispatcher"/> class.
/// </summary>
public class AlteredWorkflowDispatcher(IWorkflowDispatcher workflowDispatcher) : IAlteredWorkflowDispatcher
{
    private readonly IWorkflowDispatcher _workflowDispatcher = workflowDispatcher;

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<RunAlterationsResult> results, CancellationToken cancellationToken = default)
    {
        foreach (var result in results.Where(x => x is { IsSuccessful: true, WorkflowHasScheduledWork: true })) 
            await DispatchAsync(result, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchAsync(RunAlterationsResult result, CancellationToken cancellationToken = default) => 
        await _workflowDispatcher.DispatchAsync(new DispatchWorkflowInstanceRequest(result.WorkflowInstanceId), cancellationToken: cancellationToken);
}