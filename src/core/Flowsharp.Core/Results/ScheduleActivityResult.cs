using Flowsharp.Activities;
using Flowsharp.Models;

namespace Flowsharp.Results
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