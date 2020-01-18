using Elsa.Services.Models;

namespace Elsa.Messaging.Domain
{
    public class ActivityExecuted : ActivityNotification
    {
        public ActivityExecuted(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }
    }
}