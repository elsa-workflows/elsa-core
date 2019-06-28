using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.Results
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
                workflowContext.Workflow.Status = WorkflowStatus.Resuming;
                
                var activityInvoker = workflowContext.ServiceProvider.GetRequiredService<IActivityInvoker>();
                var result = await activityInvoker.ExecuteAsync(workflowContext, activity, cancellationToken);
                
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
