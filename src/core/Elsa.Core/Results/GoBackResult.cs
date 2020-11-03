using System.Linq;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class GoBackResult : ActivityExecutionResult
    {
        private readonly int steps;

        public GoBackResult(int steps = 1)
        {
            this.steps = steps;
        }
        
        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            var previousEntry = workflowContext.Workflow.ExecutionLog.Skip(steps).FirstOrDefault();

            if (previousEntry == null)
                return;

            var activityId = previousEntry.ActivityId;
            var activity = workflowContext.Workflow.GetActivity(activityId);
            workflowContext.ScheduleActivity(activity);
        }
    }
}