using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public abstract class ActivityNotification : INotification
    {
        protected ActivityNotification(ActivityExecutionContext activityExecutionContext, IActivity activity)
        {
            ActivityExecutionContext = activityExecutionContext;
            Activity = activity;
        }

        public ActivityExecutionContext ActivityExecutionContext { get; }
        public IActivity Activity { get; }
        public WorkflowExecutionContext WorkflowExecutionContext => ActivityExecutionContext.WorkflowExecutionContext;
        public IActivityBlueprint ActivityBlueprint => ActivityExecutionContext.ActivityBlueprint;
    }
}