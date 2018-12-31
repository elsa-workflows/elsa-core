using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Results
{
    /// <summary>
    /// Halts workflow execution.
    /// </summary>
    public class HaltResult : ActivityExecutionResult
    {
        public override async Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {            
            if (workflowContext.IsFirstPass)
            {
                var activity = workflowContext.CurrentActivity;
                var activityContext = workflowContext.CreateActivityExecutionContext(activity);
                var result = await activityContext.Descriptor.ResumeAsync(activityContext, workflowContext, cancellationToken);
                workflowContext.IsFirstPass = false;

                await result.ExecuteAsync(invoker, workflowContext, cancellationToken);
            }
            else
            {
                workflowContext.Halt();
            }
        }
    }
}
