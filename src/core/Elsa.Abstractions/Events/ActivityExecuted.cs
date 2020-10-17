using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityExecuted : ActivityNotification
    {
        public ActivityExecuted(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }
    }
}