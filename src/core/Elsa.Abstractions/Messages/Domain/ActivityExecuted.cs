using Elsa.Services.Models;

namespace Elsa.Messages.Domain
{
    public class ActivityExecuted : ActivityNotification
    {
        public ActivityExecuted(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }
    }
}