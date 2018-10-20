using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.Results
{
    public abstract class ActivityExecutionResult : IActivityExecutionResult
    {
        public virtual Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            Execute(invoker, workflowContext);
            return Task.CompletedTask;
        }

        protected virtual void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
        }
    }
}
