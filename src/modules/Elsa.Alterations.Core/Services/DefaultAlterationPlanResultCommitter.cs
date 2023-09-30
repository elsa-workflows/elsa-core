using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Results;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
public class DefaultAlterationPlanResultCommitter : IAlterationPlanResultCommitter
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowStateExtractor _workflowStateExtractor;
    private readonly IWorkflowDispatcher _workflowDispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationPlanResultCommitter"/> class.
    /// </summary>
    public DefaultAlterationPlanResultCommitter(IWorkflowRuntime workflowRuntime, IWorkflowStateExtractor workflowStateExtractor, IWorkflowDispatcher workflowDispatcher)
    {
        _workflowRuntime = workflowRuntime;
        _workflowStateExtractor = workflowStateExtractor;
        _workflowDispatcher = workflowDispatcher;
    }

    /// <inheritdoc />
    public async Task CommitAsync(AlterationPlanExecutionResult result, CancellationToken cancellationToken = default)
    {
        if (!result.HasSucceeded)
            return;

        foreach (var workflowExecutionContext in result.ModifiedWorkflowExecutionContexts)
        {
            var workflowState = _workflowStateExtractor.Extract(workflowExecutionContext);
            await _workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);
            
            // Schedule workflow for execution.
            var dispatchWorkflowRequest = new DispatchWorkflowInstanceRequest(workflowExecutionContext.Id);
            await _workflowDispatcher.DispatchAsync(dispatchWorkflowRequest, cancellationToken);
        }
    }
}