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
        public override async Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {            
            var activity = workflowContext.CurrentActivity;

            if (workflowContext.IsFirstPass)
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
