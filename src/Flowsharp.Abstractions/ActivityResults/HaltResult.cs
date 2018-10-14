using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;

namespace Flowsharp.ActivityResults
{
    /// <summary>
    /// Halts workflow execution.
    /// </summary>
    public class HaltResult : ActivityExecutionResult
    {
        public override async Task ExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var currentActivity = workflowContext.CurrentActivity;
            
            if (workflowContext.IsFirstPass)
            {
                // Resume immediately when this is the first pass.
                var result = await currentActivity.ResumeAsync(workflowContext, new ActivityExecutionContext(currentActivity), cancellationToken);
                workflowContext.IsFirstPass = false;

                await result.ExecuteAsync(workflowContext, cancellationToken);
            }
            else
            {
                await workflowContext.HaltAsync(cancellationToken);
            }
        }
    }
}
