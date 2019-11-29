using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public abstract class ActivityExecutionResult : IActivityExecutionResult
    {
        public virtual Task ExecuteAsync(IWorkflowRunner runner, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            Execute(runner, workflowContext);
            return Task.CompletedTask;
        }

        protected virtual void Execute(IWorkflowRunner runner, WorkflowExecutionContext workflowContext)
        {
        }
    }
}
