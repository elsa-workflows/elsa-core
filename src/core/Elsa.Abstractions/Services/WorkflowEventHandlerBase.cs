using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class WorkflowEventHandlerBase : IWorkflowEventHandler
    {
        public virtual Task ActivityExecutedAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, CancellationToken cancellationToken)
        {
            ActivityExecuted(workflowExecutionContext, activity);
            return Task.CompletedTask;
        }

        public virtual Task InvokingHaltedActivitiesAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            InvokingHaltedActivities(workflowExecutionContext);
            return Task.CompletedTask;
        }

        public virtual Task WorkflowInvokedAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            WorkflowInvoked(workflowExecutionContext);
            return Task.CompletedTask;
        }

        protected virtual void ActivityExecuted(WorkflowExecutionContext workflowExecutionContext, IActivity activity)
        {
        }
        
        protected virtual void InvokingHaltedActivities(WorkflowExecutionContext workflowExecutionContext)
        {
        }
        
        protected virtual void WorkflowInvoked(WorkflowExecutionContext workflowExecutionContext)
        {
        }
    }
}