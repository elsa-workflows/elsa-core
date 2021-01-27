using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityResuming : ActivityNotification
    {
        public ActivityResuming(ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
        }
    }
}