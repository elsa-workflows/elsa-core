using Elsa.Services.Models;

namespace Elsa.Events
{
    public class ActivityExecuted : ActivityNotification
    {
        public ActivityExecuted(bool resuming, ActivityExecutionContext activityExecutionContext) : base(activityExecutionContext) => Resuming = resuming;
        public bool Resuming { get; }
    }
}