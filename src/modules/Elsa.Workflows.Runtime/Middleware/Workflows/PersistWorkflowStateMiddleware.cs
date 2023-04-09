using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of persisting workflow execution log entries.
/// </summary>
public class PersistWorkflowStateMiddleware : WorkflowExecutionMiddleware
{
    private readonly IWorkflowStateStore _workflowStateStore;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;

    /// <inheritdoc />
    public PersistWorkflowStateMiddleware(WorkflowMiddlewareDelegate next, IWorkflowStateStore workflowStateStore, IWorkflowStateSerializer workflowStateSerializer) : base(next)
    {
        _workflowStateStore = workflowStateStore;
        _workflowStateSerializer = workflowStateSerializer;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Extract workflow state.
        var workflowState = _workflowStateSerializer.SerializeState(context);

        // Store the serializable state in context for the pipeline caller.
        context.TransientProperties[context] = workflowState;
        
        // Persist workflow state.
        await _workflowStateStore.SaveAsync(context.Id, workflowState, context.CancellationToken);
    }
}