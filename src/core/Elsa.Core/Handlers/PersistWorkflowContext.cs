using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Handlers
{
    public class PersistWorkflowContext : INotificationHandler<WorkflowExecuted>, INotificationHandler<ActivityExecuted>
    {
        private readonly IWorkflowContextManager _workflowContextManager;

        public PersistWorkflowContext(IWorkflowContextManager workflowContextManager)
        {
            _workflowContextManager = workflowContextManager;
        }


        public async Task Handle(WorkflowExecuted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            workflowInstance.ContextId = await SaveWorkflowContextAsync(workflowExecutionContext, WorkflowContextFidelity.Burst, false, cancellationToken);
        }

        public async Task Handle(ActivityExecuted notification, CancellationToken cancellationToken)
        {
            var workflowExecutionContext = notification.WorkflowExecutionContext;
            var activityBlueprint = notification.ActivityBlueprint;
            workflowExecutionContext.WorkflowInstance.ContextId = await SaveWorkflowContextAsync(workflowExecutionContext, WorkflowContextFidelity.Activity, activityBlueprint.SaveWorkflowContext, cancellationToken);
        }
        
        private async ValueTask<string?> SaveWorkflowContextAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowContextFidelity fidelity, bool always, CancellationToken cancellationToken)
        {
            var workflowContext = workflowExecutionContext.WorkflowContext;

            if (!always && (workflowContext == null || workflowExecutionContext.WorkflowBlueprint.ContextOptions?.ContextFidelity != fidelity))
                return workflowExecutionContext.WorkflowInstance.ContextId;

            var context = new SaveWorkflowContext(workflowExecutionContext);
            return await _workflowContextManager.SaveContextAsync(context, cancellationToken);
        }
    }
}