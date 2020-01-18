using Elsa.Services.Models;

namespace Elsa.Messages.Domain
{
    public class ActivityExecuting : ActivityNotification
    {
        public ActivityExecuting(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }
    }
}