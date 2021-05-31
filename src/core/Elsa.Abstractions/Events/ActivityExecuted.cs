using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityExecuted : ActivityNotification
    {
        public ActivityExecuted(ActivityExecutionContext activityExecutionContext, IActivity activity) : base(activityExecutionContext, activity)
        {
        }

        public bool Resuming => ActivityExecutionContext.Resuming;
    }
}