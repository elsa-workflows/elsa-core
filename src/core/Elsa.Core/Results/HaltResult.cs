using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using NodaTime;

namespace Elsa.Results
{
    /// <summary>
    /// Halts workflow execution.
    /// </summary>
    public class HaltResult : ActivityExecutionResult
    {
        private readonly Instant instant;

        public HaltResult(Instant instant)
        {
            this.instant = instant;
        }
        
        public override async Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {            
            var activity = workflowContext.CurrentActivity;

            if (workflowContext.IsFirstPass)
            {
                var result = await invoker.ActivityInvoker.ResumeAsync(workflowContext, activity, cancellationToken);
                workflowContext.IsFirstPass = false;

                await result.ExecuteAsync(invoker, workflowContext, cancellationToken);
            }
            else
            {
                workflowContext.ScheduleHaltingActivity(activity);
                workflowContext.Halt(instant);
            }
        }
    }
}
