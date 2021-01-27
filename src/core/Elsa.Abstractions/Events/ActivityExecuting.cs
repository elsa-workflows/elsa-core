using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityExecuting : ActivityNotification
    {
        public ActivityExecuting(bool resuming, ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext)
        {
            Resuming = resuming;
        }
        
        public bool Resuming { get; }

    }
}