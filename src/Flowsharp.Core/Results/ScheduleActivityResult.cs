using Flowsharp.Activities;
using Flowsharp.Models;

namespace Flowsharp.ActivityResults
{
    public class ScheduleActivityResult : ActivityExecutionResult
    {
        private readonly IActivity activity;

        public ScheduleActivityResult(IActivity activity)
        {
            this.activity = activity;
        }
        
        protected override void Execute(WorkflowExecutionContext workflowContext)
        {
            workflowContext.ScheduleActivity(activity);
        }
    }
}