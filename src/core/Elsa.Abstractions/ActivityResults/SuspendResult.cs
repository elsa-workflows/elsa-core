using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class SuspendResult : ActivityExecutionResult
    {
        protected override void Execute(ActivityExecutionContext activityExecutionContext)
        {
            var activityDefinition = activityExecutionContext.ActivityDefinition;
            var blockingActivity = new BlockingActivity(activityDefinition.Id, activityDefinition.Type);
            activityExecutionContext.WorkflowExecutionContext.BlockingActivities.Add(blockingActivity);
            activityExecutionContext.WorkflowExecutionContext.Suspend();
        }
    }
}