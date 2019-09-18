using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Results
{
    /// <summary>
    /// Halts workflow execution.
    /// </summary>
    public class HaltResult : ActivityExecutionResult
    {
        public HaltResult(bool continueOnFirstPass = false)
        {
            ContinueOnFirstPass = continueOnFirstPass;
        }
        
        public bool ContinueOnFirstPass { get; }
        
        public override async Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {            
            var activity = workflowContext.CurrentActivity;

            if (workflowContext.IsFirstPass && ContinueOnFirstPass)
            {
                var activityInvoker = workflowContext.ServiceProvider.GetRequiredService<IActivityInvoker>();
                var result = await activityInvoker.ResumeAsync(workflowContext, activity, cancellationToken);
                
                workflowContext.IsFirstPass = false;

                await result.ExecuteAsync(invoker, workflowContext, cancellationToken);
            }
            else
            {
                workflowContext.ScheduleHaltingActivity(activity);
                workflowContext.Halt(activity);
            }
        }
    }
}
