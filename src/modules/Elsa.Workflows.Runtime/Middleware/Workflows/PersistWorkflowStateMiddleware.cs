using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of persisting workflow execution log entries.
/// </summary>
public class PersistWorkflowStateMiddleware : WorkflowExecutionMiddleware
{
    private readonly IWorkflowStateStore _workflowStateStore;
    private readonly IWorkflowExecutionContextMapper _workflowExecutionContextMapper;
    private readonly INotificationSender _notificationSender;
    private readonly ISystemClock _systemClock;

    /// <inheritdoc />
    public PersistWorkflowStateMiddleware(
        WorkflowMiddlewareDelegate next, 
        IWorkflowStateStore workflowStateStore, 
        IWorkflowExecutionContextMapper workflowExecutionContextMapper,
        INotificationSender notificationSender,
        ISystemClock systemClock) : base(next)
    {
        _workflowStateStore = workflowStateStore;
        _workflowExecutionContextMapper = workflowExecutionContextMapper;
        _notificationSender = notificationSender;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Extract workflow state.
        var workflowState = _workflowExecutionContextMapper.Extract(context);
        var now = _systemClock.UtcNow;
        
        if(context.Status == WorkflowStatus.Finished) 
            workflowState.FinishedAt = now;
        
        workflowState.UpdatedAt = now;
        
        // Store the serializable state in context for the pipeline caller.
        context.TransientProperties[context] = workflowState;
        
        // Persist workflow state.
        await _workflowStateStore.SaveAsync(context.Id, workflowState, context.CancellationToken);
    }
}