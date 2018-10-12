using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.ActivityResults
{
    public abstract class ActivityExecutionResult : IActivityExecutionResult
    {
        public virtual Task ExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            Execute(workflowContext);
            return Task.CompletedTask;
        }

        protected virtual void Execute(WorkflowExecutionContext workflowContext)
        {
            throw new NotImplementedException("You must either implement ExecuteAsync or Execute");
        }
    }
}
