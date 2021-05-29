using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityResuming : ActivityNotification
    {
        public ActivityResuming(ActivityExecutionContext activityExecutionContext, IActivity activity) : base(activityExecutionContext, activity)
        {
        }
    }
}