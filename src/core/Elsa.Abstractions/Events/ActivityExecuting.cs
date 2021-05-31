using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityExecuting : ActivityNotification
    {
        public ActivityExecuting(ActivityExecutionContext activityExecutionContext, IActivity activity) : base(activityExecutionContext, activity)
        {
        }

        public bool Resuming => ActivityExecutionContext.Resuming;

    }
}