using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityExecuting : ActivityNotification
    {
        public ActivityExecuting(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }
    }
}