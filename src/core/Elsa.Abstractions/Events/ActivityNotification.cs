using Elsa.Services.Models;
using MediatR;

namespace Elsa.Events
{
    public abstract class ActivityNotification : INotification
    {
        protected ActivityNotification(ActivityExecutionContext activityExecutionContext)
        {
            ActivityExecutionContext = activityExecutionContext;
        }

        public ActivityExecutionContext ActivityExecutionContext { get; }
        public WorkflowExecutionContext WorkflowExecutionContext => ActivityExecutionContext.WorkflowExecutionContext;
        public IActivityBlueprint Activity => ActivityExecutionContext.ActivityBlueprint;
    }
}