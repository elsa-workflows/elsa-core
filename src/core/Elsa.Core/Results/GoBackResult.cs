using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Results
{
    public class GoBackResult : ActivityExecutionResult
    {
        private readonly int steps;

        public GoBackResult(int steps = 1)
        {
            this.steps = steps;
        }

        public override async Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var currentActivity = workflowContext.CurrentActivity;
            var previousEntry = workflowContext.Workflow.ExecutionLog.OrderByDescending(x => x.Timestamp).Skip(steps).FirstOrDefault();

            if (previousEntry == null)
                return;

            var activityId = previousEntry.ActivityId;
            var activity = workflowContext.Workflow.GetActivity(activityId);
            workflowContext.ScheduleActivity(activity);

            var eventHandlers = workflowContext.ServiceProvider.GetServices<IWorkflowEventHandler>();
            var logger = workflowContext.ServiceProvider.GetRequiredService<ILogger<GoBackResult>>();
            await eventHandlers.InvokeAsync(x => x.ActivityExecutedAsync(workflowContext, currentActivity, cancellationToken), logger);
        }
    }
}