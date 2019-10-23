using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class ScheduleActivityResult : ActivityExecutionResult
    {
        private readonly IActivity activity;

        public ScheduleActivityResult(IActivity activity)
        {
            this.activity = activity;
        }
        
        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            workflowContext.ScheduleActivity(activity);
        }
    }
}