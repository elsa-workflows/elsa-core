using Elsa.Services.Models;

namespace Elsa.Messages
{
    public class ExecutingActivity : WorkflowNotification
    {
        public ExecutingActivity(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext) : base(workflowExecutionContext)
        {
            ActivityExecutionContext = activityExecutionContext;
        }
        
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }
}