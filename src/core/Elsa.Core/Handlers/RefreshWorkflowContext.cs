using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Handlers
{
    public class RefreshWorkflowContext : INotificationHandler<WorkflowExecuting>, INotificationHandler<ActivityActivating>
    {
        private readonly IWorkflowContextManager _workflowContextManager;

        public RefreshWorkflowContext(IWorkflowContextManager workflowContextManager)
        {
            _workflowContextManager = workflowContextManager;
        }
        
        public async Task Handle(WorkflowExecuting notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            workflowExecutionContext.WorkflowContext = await LoadWorkflowContextAsync(workflowExecutionContext, default, true, cancellationToken);
        }
    
        public async Task Handle(ActivityActivating notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;
            var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;
            var activityBlueprint = activityExecutionContext.ActivityBlueprint;
            
            if (workflowBlueprint.ContextOptions?.ContextFidelity == WorkflowContextFidelity.Activity || activityBlueprint.LoadWorkflowContext || workflowExecutionContext.ContextHasChanged)
            {
                workflowExecutionContext.WorkflowContext = await LoadWorkflowContextAsync(workflowExecutionContext, WorkflowContextFidelity.Activity, activityBlueprint.LoadWorkflowContext || workflowExecutionContext.ContextHasChanged, cancellationToken);
                workflowExecutionContext.ContextHasChanged = false;
            }
        }
        
        private async ValueTask<object?> LoadWorkflowContextAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowContextFidelity? fidelity, bool always, CancellationToken cancellationToken)
        {
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            var workflowBlueprint = workflowExecutionContext.WorkflowBlueprint;

            if (!always && (workflowInstance.ContextId == null || workflowBlueprint.ContextOptions == null || workflowBlueprint.ContextOptions.ContextFidelity != fidelity))
                return null;

            var context = new LoadWorkflowContext(workflowExecutionContext);
            return await _workflowContextManager.LoadContext(context, cancellationToken);
        }
    }
}