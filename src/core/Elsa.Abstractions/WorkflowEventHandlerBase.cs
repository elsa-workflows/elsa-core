using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa
{
    public abstract class WorkflowEventHandlerBase : IWorkflowEventHandler
    {
        public virtual Task ActivityExecutedAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, CancellationToken cancellationToken)
        {
            ActivityExecuted(workflowExecutionContext, activity);
            return Task.CompletedTask;
        }

        protected virtual void ActivityExecuted(WorkflowExecutionContext workflowExecutionContext, IActivity activity)
        {
        }
    }
}