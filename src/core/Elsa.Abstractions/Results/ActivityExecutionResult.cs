using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
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
