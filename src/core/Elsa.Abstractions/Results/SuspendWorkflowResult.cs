using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Results
{
    /// <summary>
    /// Suspends workflow execution.
    /// </summary>
    public class SuspendWorkflowResult : ActivityExecutionResult
    {
        public SuspendWorkflowResult(bool continueOnFirstPass = false)
        {
            ContinueOnFirstPass = continueOnFirstPass;
        }
        
        public bool ContinueOnFirstPass { get; }
        
        public override async Task ExecuteAsync(IProcessRunner runner, ProcessExecutionContext processContext, CancellationToken cancellationToken)
        {            
            var activity = processContext.ScheduledActivity.Activity;
            var input = processContext.ScheduledActivity.Input;

            if (processContext.IsFirstPass && ContinueOnFirstPass)
            {
                var activityInvoker = processContext.ServiceProvider.GetRequiredService<IActivityInvoker>();
                var result = await activityInvoker.ResumeAsync(processContext, activity, input, cancellationToken);
                
                processContext.IsFirstPass = false;

                await result.ExecuteAsync(runner, processContext, cancellationToken);
            }
            else
                processContext.AddBlockingActivity(activity);
        }
    }
}
