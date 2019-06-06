using Elsa.Models;
using Elsa.Results;

namespace Elsa.Core.Results
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