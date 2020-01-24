using Elsa.Services.Models;

namespace Elsa.Messaging.Domain
{
    public class ActivityExecuting : ActivityNotification
    {
        public ActivityExecuting(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }
    }
}