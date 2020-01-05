using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class SuspendResult : IActivityExecutionResult
    {
        public Task ExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            workflowExecutionContext.BlockingActivities.Add(activityExecutionContext.Activity);
            workflowExecutionContext.Suspend();
            return Task.CompletedTask;
        }
    }
}